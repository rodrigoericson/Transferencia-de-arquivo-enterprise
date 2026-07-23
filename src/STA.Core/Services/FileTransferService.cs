using Microsoft.Extensions.Logging;
using STA.Core.Data.Entities;
using STA.Core.Data.Repositories;
using STA.Core.Models;
using STA.Core.Services.Transports;
using System.Diagnostics;

namespace STA.Core.Services;

public record FileTransferResult(
    int FilesProcessed,
    int FilesSucceeded,
    int FilesFailed,
    List<string> ErrorMessages);

public record DestinoTransfer(string Diretorio, string? PadraoRename, RotaDestino? Destino = null, ConexaoSftp? Conexao = null);

public interface IFileTransferService
{
    Task<FileTransferResult> TransferFanOutAsync(
        TransferPath config,
        string sourceDirectory,
        IReadOnlyList<DestinoTransfer> destinos,
        bool overwriteExisting,
        int timeoutCompactacaoMs,
        int? cnLogProcesso,
        CancellationToken cancellationToken);
}

public class FileTransferService : IFileTransferService
{
    private readonly IFileMaskMatcher _maskMatcher;
    private readonly IFileSizeValidator _sizeValidator;
    private readonly IFileLockChecker _lockChecker;
    private readonly IFileCompressor _compressor;
    private readonly IFilePurgeService _purgeService;
    private readonly ILogArquivoRepository _logArquivoRepository;
    private readonly ILogSftpRepository _logSftpRepository;
    private readonly ITransportFactory _transportFactory;
    private readonly ILogger<FileTransferService> _logger;
    private SftpConnectionPool? _sftpPool;

    public FileTransferService(
        IFileMaskMatcher maskMatcher,
        IFileSizeValidator sizeValidator,
        IFileLockChecker lockChecker,
        IFileCompressor compressor,
        IFilePurgeService purgeService,
        ILogArquivoRepository logArquivoRepository,
        ILogSftpRepository logSftpRepository,
        ITransportFactory transportFactory,
        ILogger<FileTransferService> logger)
    {
        _maskMatcher = maskMatcher;
        _sizeValidator = sizeValidator;
        _lockChecker = lockChecker;
        _compressor = compressor;
        _purgeService = purgeService;
        _logArquivoRepository = logArquivoRepository;
        _logSftpRepository = logSftpRepository;
        _transportFactory = transportFactory;
        _logger = logger;
    }

    public void SetSftpPool(SftpConnectionPool? pool) => _sftpPool = pool;

    public async Task<FileTransferResult> TransferFanOutAsync(
        TransferPath config,
        string sourceDirectory,
        IReadOnlyList<DestinoTransfer> destinos,
        bool overwriteExisting,
        int timeoutCompactacaoMs,
        int? cnLogProcesso,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            _logger.LogWarning("Diretório de origem não existe: '{Path}'.", sourceDirectory);
            return new FileTransferResult(0, 0, 0, [$"Diretório de origem não existe: {sourceDirectory}"]);
        }

        foreach (var dest in destinos)
        {
            if (dest.Destino?.IdProtocolo == "SFTP")
                continue;
            try { Directory.CreateDirectory(dest.Diretorio); }
            catch (Exception ex) { _logger.LogWarning(ex, "Não foi possível criar diretório de destino: '{Path}'.", dest.Diretorio); }
        }
        if (!string.IsNullOrWhiteSpace(config.DiretorioBackup))
        {
            try { Directory.CreateDirectory(config.DiretorioBackup); }
            catch (Exception ex) { _logger.LogWarning(ex, "Não foi possível criar diretório de backup: '{Path}'.", config.DiretorioBackup); }
        }

        if (destinos.Count == 0)
        {
            _logger.LogWarning("Rota sem destinos ativos. Nenhum arquivo será processado (origem preservada).");
            return new FileTransferResult(0, 0, 1, ["Rota sem destinos ativos — configuração incorreta."]);
        }

        var errors = new List<string>();
        int succeeded = 0, failed = 0;
        var files = new DirectoryInfo(sourceDirectory).GetFiles();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_maskMatcher.Match(file.Name, config.MascaraArq))
                continue;

            if (_lockChecker.IsFileLocked(file.FullName))
            {
                _logger.LogWarning("Arquivo em uso, ignorado: '{File}'.", file.Name);
                await GravarLogArquivoAsync(
                    cnLogProcesso, config, file.Name, sourceDirectory, destinos.FirstOrDefault()?.Diretorio ?? "",
                    file.Length, DateTime.UtcNow, "W", "Arquivo em uso (locked) — será tentado no próximo ciclo", false, false, cancellationToken);
                continue;
            }

            if (!_sizeValidator.IsWithinRange(file.Length, config.TamanhoInicialArqBytes, config.TamanhoFinalArqBytes))
            {
                _logger.LogDebug("Arquivo fora da faixa de tamanho: '{File}' ({Size} bytes).", file.Name, file.Length);
                continue;
            }

            // Processa 1 arquivo por vez: backup + fan-out para todos os destinos + apaga origem só no final
            var dtInicioArquivo = DateTime.UtcNow;
            bool compressed = false;
            try
            {
                var (filePath, fileName, wasCompressed) = await TryCompressAsync(file, config, sourceDirectory, timeoutCompactacaoMs, cancellationToken);
                compressed = wasCompressed;

                // 1. Backup (se configurado) — se falhar e backup era esperado, não apaga origem
                bool backupOk = CopyToBackup(filePath, fileName, config.DiretorioBackup, overwriteExisting);

                // 2. Fan-out: copia para TODOS os destinos, rastreia resultado por destino
                bool fanOutOk = true;
                var destResults = new List<(string Dir, bool Ok, string? Erro)>();
                var resolvedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var dest in destinos)
                {
                    try
                    {
                        var destFileName = AplicarRename(fileName, dest.PadraoRename);
                        if (destFileName.Contains("..") || destFileName.Contains('/') || destFileName.Contains('\\'))
                        {
                            _logger.LogWarning("Rename pattern gerou nome inseguro: '{Name}'. Usando nome original.", destFileName);
                            destFileName = fileName;
                        }
                        var destIdentity = dest.Destino?.IdProtocolo == "SFTP" ? $"SFTP:{dest.Destino?.CnRotaDestino}" : $"LOCAL:{dest.Diretorio}";
                        var destKey = $"{destIdentity}|{destFileName}";
                        if (!resolvedNames.Add(destKey))
                        {
                            _logger.LogWarning("Colisão de nome no destino: '{Dir}/{Name}'. Arquivo ignorado para este destino.", dest.Diretorio, destFileName);
                            destResults.Add((dest.Diretorio, false, $"Colisão de rename: {destFileName}"));
                            fanOutOk = false;
                            continue;
                        }
                        if (dest.Destino != null && dest.Destino.IdProtocolo == "SFTP")
                        {
                            var remotePath = CombinePosixPath(dest.Diretorio, destFileName);
                            var transport = _transportFactory.Criar(dest.Destino, dest.Conexao, _sftpPool);
                            var sw = Stopwatch.StartNew();
                            await transport.UploadFileAsync(filePath, remotePath, overwriteExisting, cancellationToken);
                            sw.Stop();

                            if (dest.Conexao != null)
                            {
                                try { await _logSftpRepository.InserirAsync(new LogSftp
                                {
                                    CnConexaoSftp = dest.Conexao.CnConexaoSftp,
                                    CnRotaDestino = dest.Destino.CnRotaDestino,
                                    IdTipo = "UPLOAD",
                                    IdStatus = "S",
                                    NmArquivo = destFileName,
                                    NrTamanhoBytes = file.Length,
                                    NrDuracaoMs = (int)sw.ElapsedMilliseconds,
                                    DsMensagem = $"{dest.Conexao.DsHost}:{dest.Conexao.NrPorta}{remotePath}",
                                    DtEvento = DateTime.UtcNow
                                }, cancellationToken); } catch { }
                            }
                        }
                        else
                        {
                            CopyToDestination(filePath, destFileName, dest.Diretorio, overwriteExisting);
                        }

                        destResults.Add((dest.Diretorio, true, null));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao copiar '{File}' para '{Dest}'.", fileName, dest.Diretorio);
                        errors.Add($"{file.Name} → {dest.Diretorio}: {ex.Message}");
                        destResults.Add((dest.Diretorio, false, ex.Message));

                        if (dest.Destino?.IdProtocolo == "SFTP" && dest.Conexao != null)
                        {
                            try { await _logSftpRepository.InserirAsync(new LogSftp
                            {
                                CnConexaoSftp = dest.Conexao.CnConexaoSftp,
                                CnRotaDestino = dest.Destino.CnRotaDestino,
                                IdTipo = "ERRO",
                                IdStatus = "E",
                                NmArquivo = file.Name,
                                DsMensagem = ex.Message,
                                DtEvento = DateTime.UtcNow
                            }, cancellationToken); } catch { }
                        }
                        fanOutOk = false;
                    }
                }

                // 3. Só apaga origem se fan-out foi 100% bem-sucedido, backup OK e flag permite
                if (fanOutOk && backupOk && config.FlExcluirOrigem)
                {
                    CleanupSource(file.FullName, filePath);
                }

                // 4. Grava log por destino (S ou E conforme resultado)
                foreach (var (destDir, ok, erro) in destResults)
                {
                    await GravarLogArquivoAsync(
                        cnLogProcesso, config, file.Name, sourceDirectory, destDir,
                        file.Length, dtInicioArquivo, ok ? "S" : "E", erro, compressed, false, cancellationToken);
                }

                if (!fanOutOk) failed++;
                else succeeded++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                errors.Add($"{file.Name}: {ex.Message}");
                _logger.LogError(ex, "Erro ao transferir '{File}'.", file.Name);
                await GravarLogArquivoAsync(
                    cnLogProcesso, config, file.Name, sourceDirectory, destinos.FirstOrDefault()?.Diretorio ?? "",
                    file.Length, dtInicioArquivo, "E", ex.Message, compressed, false, cancellationToken);
            }
        }

        PurgeBackupIfNeeded(config);

        return new FileTransferResult(files.Length, succeeded, failed, errors);
    }

    private async Task<(string FilePath, string FileName, bool Compressed)> TryCompressAsync(
        FileInfo file,
        TransferPath config,
        string sourceDirectory,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (!_compressor.IsCompressionTypeSupported(config.CompactaOrigemTipo)
            || _compressor.IsFileCompressed(file.FullName))
            return (file.FullName, file.Name, false);

        var archiveName = file.Name + "." + config.CompactaOrigemTipo.ToLowerInvariant();
        var archivePath = Path.Combine(sourceDirectory, archiveName);

        var ok = await _compressor.CompressAsync(
            file.FullName, archivePath, config.CompactaOrigemTipo, timeoutMs, cancellationToken);

        if (ok)
            return (archivePath, archiveName, true);

        _logger.LogWarning("Falha na compactação de '{File}'. Transferindo original.", file.Name);
        return (file.FullName, file.Name, false);
    }

    private static void CopyToDestination(string sourceFilePath, string fileName, string destDir, bool overwrite)
    {
        var destPath = Path.Combine(destDir, fileName);
        File.Copy(sourceFilePath, destPath, overwrite);
    }

    private bool CopyToBackup(string sourceFilePath, string fileName, string backupDir, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(backupDir))
            return true;

        try
        {
            var backupPath = Path.Combine(backupDir, fileName);
            File.Copy(sourceFilePath, backupPath, overwrite);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao criar backup de '{File}'. Origem não será removida.", fileName);
            return false;
        }
    }

    private static void CleanupSource(string originalPath, string transferredPath)
    {
        if (transferredPath != originalPath && File.Exists(transferredPath))
            File.Delete(transferredPath);

        if (File.Exists(originalPath))
            File.Delete(originalPath);
    }

    private static string CombinePosixPath(string directory, string fileName)
    {
        var dir = directory.TrimEnd('/');
        return $"{dir}/{fileName}";
    }

    private static string AplicarRename(string originalFileName, string? padrao)
    {
        if (string.IsNullOrWhiteSpace(padrao))
            return originalFileName;

        var name = Path.GetFileNameWithoutExtension(originalFileName);
        var ext = Path.GetExtension(originalFileName).TrimStart('.');
        var now = DateTime.Now;

        var result = padrao
            .Replace("{NAME}", name, StringComparison.OrdinalIgnoreCase)
            .Replace("{EXT}", ext, StringComparison.OrdinalIgnoreCase)
            .Replace("{DATE}", now.ToString("yyyyMMdd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{YEAR}", now.ToString("yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{MONTH}", now.ToString("MM"), StringComparison.OrdinalIgnoreCase)
            .Replace("{DAY}", now.ToString("dd"), StringComparison.OrdinalIgnoreCase)
            .Replace("{TIME}", now.ToString("HHmmss"), StringComparison.OrdinalIgnoreCase);

        return string.IsNullOrWhiteSpace(result) ? originalFileName : result;
    }

    private void PurgeBackupIfNeeded(TransferPath config)
    {
        if (string.IsNullOrWhiteSpace(config.DiretorioBackup) || config.DiasExcluir <= 0)
            return;

        var cutoff = DateTime.Now.AddDays(-config.DiasExcluir);
        _purgeService.PurgeDirectory(config.DiretorioBackup, cutoff, config.MascaraArq);
    }

    private async Task GravarLogArquivoAsync(
        int? cnLogProcesso,
        TransferPath config,
        string nomeArquivo,
        string diretorioOrigem,
        string diretorioDestino,
        long tamanhoBytes,
        DateTime dtInicio,
        string status,
        string? mensagem,
        bool compactado,
        bool descompactado,
        CancellationToken cancellationToken)
    {
        try
        {
            var log = new LogArquivo
            {
                CnLogProcesso = cnLogProcesso,
                CnEtapa = config.CnEtapa,
                CnRota = config.CnRota,
                NmArquivo = nomeArquivo,
                DsDiretorioOrigem = diretorioOrigem,
                DsDiretorioDestino = diretorioDestino,
                NrTamanhoBytes = tamanhoBytes,
                DtInicio = dtInicio,
                DtFim = DateTime.UtcNow,
                IdStatus = status,
                DsMensagem = mensagem,
                FlCompactado = compactado,
                FlDescompactado = descompactado
            };

            await _logArquivoRepository.InserirAsync(log, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao gravar log de arquivo '{File}'.", nomeArquivo);
        }
    }
}
