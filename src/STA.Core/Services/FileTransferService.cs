using Microsoft.Extensions.Logging;
using STA.Core.Data.Entities;
using STA.Core.Data.Repositories;
using STA.Core.Models;
using STA.Core.Services.Transports;

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
    private readonly ITransportFactory _transportFactory;
    private readonly ILogger<FileTransferService> _logger;

    public FileTransferService(
        IFileMaskMatcher maskMatcher,
        IFileSizeValidator sizeValidator,
        IFileLockChecker lockChecker,
        IFileCompressor compressor,
        IFilePurgeService purgeService,
        ILogArquivoRepository logArquivoRepository,
        ITransportFactory transportFactory,
        ILogger<FileTransferService> logger)
    {
        _maskMatcher = maskMatcher;
        _sizeValidator = sizeValidator;
        _lockChecker = lockChecker;
        _compressor = compressor;
        _purgeService = purgeService;
        _logArquivoRepository = logArquivoRepository;
        _transportFactory = transportFactory;
        _logger = logger;
    }

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
            try { Directory.CreateDirectory(dest.Diretorio); }
            catch (Exception ex) { _logger.LogWarning(ex, "Não foi possível criar diretório de destino: '{Path}'.", dest.Diretorio); }
        }
        if (!string.IsNullOrWhiteSpace(config.DiretorioBackup))
        {
            try { Directory.CreateDirectory(config.DiretorioBackup); }
            catch (Exception ex) { _logger.LogWarning(ex, "Não foi possível criar diretório de backup: '{Path}'.", config.DiretorioBackup); }
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

                // 1. Backup (se configurado)
                CopyToBackup(filePath, fileName, config.DiretorioBackup, overwriteExisting);

                // 2. Fan-out: copia para TODOS os destinos, rastreia resultado por destino
                bool fanOutOk = true;
                var destResults = new List<(string Dir, bool Ok, string? Erro)>();

                foreach (var dest in destinos)
                {
                    try
                    {
                        var destFileName = AplicarRename(fileName, dest.PadraoRename);
                        var remotePath = Path.Combine(dest.Diretorio, destFileName);

                        if (dest.Destino != null)
                        {
                            var transport = _transportFactory.Criar(dest.Destino, dest.Conexao);
                            await transport.UploadFileAsync(filePath, remotePath, overwriteExisting, cancellationToken);
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
                        fanOutOk = false;
                    }
                }

                // 3. Só apaga origem se fan-out foi 100% bem-sucedido
                if (fanOutOk)
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

    private void CopyToBackup(string sourceFilePath, string fileName, string backupDir, bool overwrite)
    {
        if (string.IsNullOrWhiteSpace(backupDir))
            return;

        try
        {
            var backupPath = Path.Combine(backupDir, fileName);
            File.Copy(sourceFilePath, backupPath, overwrite);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao criar backup de '{File}'.", fileName);
        }
    }

    private async Task<bool> TryDecompressAtDestinationAsync(
        string destinationFilePath,
        string destinationDirectory,
        TransferPath config,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.DescompactaDestino)
            || !config.DescompactaDestino.Equals("SIM", StringComparison.OrdinalIgnoreCase)
            || !_compressor.IsFileCompressed(destinationFilePath))
            return false;

        var ok = await _compressor.DecompressAsync(
            destinationFilePath, destinationDirectory, timeoutMs, cancellationToken);

        if (ok)
            File.Delete(destinationFilePath);
        else
            _logger.LogWarning("Falha na descompactação de '{File}' no destino.", Path.GetFileName(destinationFilePath));

        return ok;
    }

    private static void CleanupSource(string originalPath, string transferredPath)
    {
        if (transferredPath != originalPath && File.Exists(transferredPath))
            File.Delete(transferredPath);

        if (File.Exists(originalPath))
            File.Delete(originalPath);
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
