using Microsoft.Extensions.Logging;
using STA.Core.Data.Entities;
using STA.Core.Data.Repositories;
using STA.Core.Models;
using STA.Core.Services.Transports;

namespace STA.Core.Services;

public interface IReturnDownloadService
{
    Task<FileTransferResult> ProcessarRetornoAsync(
        TransferPath config,
        ConexaoSftp conexaoRetorno,
        SftpConnectionPool pool,
        int? cnLogProcesso,
        bool isUltimoHorario,
        CancellationToken ct);
}

public class ReturnDownloadService : IReturnDownloadService
{
    private readonly IFileMaskMatcher _maskMatcher;
    private readonly IFileLockChecker _lockChecker;
    private readonly ILogSftpRepository _logSftpRepository;
    private readonly ILogArquivoRepository _logArquivoRepository;
    private readonly ILogger<ReturnDownloadService> _logger;

    public ReturnDownloadService(
        IFileMaskMatcher maskMatcher,
        IFileLockChecker lockChecker,
        ILogSftpRepository logSftpRepository,
        ILogArquivoRepository logArquivoRepository,
        ILogger<ReturnDownloadService> logger)
    {
        _maskMatcher = maskMatcher;
        _lockChecker = lockChecker;
        _logSftpRepository = logSftpRepository;
        _logArquivoRepository = logArquivoRepository;
        _logger = logger;
    }

    public async Task<FileTransferResult> ProcessarRetornoAsync(
        TransferPath config,
        ConexaoSftp conexaoRetorno,
        SftpConnectionPool pool,
        int? cnLogProcesso,
        bool isUltimoHorario,
        CancellationToken ct)
    {
        if (!config.FlHabilitarRetorno
            || string.IsNullOrWhiteSpace(config.DsDiretorioRetorno)
            || string.IsNullOrWhiteSpace(config.DsDiretorioLocalRetorno))
            return new FileTransferResult(0, 0, 0, []);

        if (!SftpPathValidator.TryNormalize(config.DsDiretorioRetorno, out var normalizedRetornoDir, out var erroPath))
        {
            _logger.LogWarning("Diretório de retorno inválido: {Erro}", erroPath);
            return new FileTransferResult(0, 0, 0, [$"Configuração inválida: {erroPath}"]);
        }

        var errors = new List<string>();
        int succeeded = 0, failed = 0;

        try
        {
            Directory.CreateDirectory(config.DsDiretorioLocalRetorno);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível criar diretório local de retorno: '{Path}'.", config.DsDiretorioLocalRetorno);
            return new FileTransferResult(0, 0, 0, [$"Erro ao criar diretório local de retorno."]);
        }

        ISftpClientWrapper client;
        try
        {
            client = pool.GetOrCreate(conexaoRetorno);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao conectar SFTP de retorno '{Nome}'.", conexaoRetorno.NmConexao);
            return new FileTransferResult(0, 0, 0, [$"Falha ao conectar SFTP retorno."]);
        }

        var transport = new SftpTransport(client, _logger as ILogger<SftpTransport> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SftpTransport>.Instance);

        List<SftpRemoteEntry> entries;
        try
        {
            entries = client.ListDirectoryDetailed(normalizedRetornoDir)
                .Where(e => !e.IsDirectory && _maskMatcher.Match(e.Name, config.DsMascaraRetorno))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao listar diretório de retorno '{Dir}'.", normalizedRetornoDir);
            return new FileTransferResult(0, 0, 0, [$"Erro ao listar diretório de retorno."]);
        }

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();

            if (entry.Name.Contains("..") || entry.Name.Contains('/') || entry.Name.Contains('\\') || Path.IsPathRooted(entry.Name))
            {
                _logger.LogWarning("Nome de arquivo remoto inseguro ignorado: '{Name}'.", entry.Name);
                continue;
            }

            var remotePath = $"{normalizedRetornoDir.TrimEnd('/')}/{entry.Name}";
            var localPath = Path.Combine(config.DsDiretorioLocalRetorno, entry.Name);

            // Fix: locked file = skip silencioso, não failure
            if (File.Exists(localPath) && _lockChecker.IsFileLocked(localPath))
            {
                _logger.LogDebug("Arquivo local de retorno em uso, será tentado no próximo ciclo: '{File}'.", entry.Name);
                continue;
            }

            // Fix: idempotência — se arquivo local já existe com mesmo tamanho, assumir já baixado e só tentar apagar remoto
            if (File.Exists(localPath))
            {
                var localSize = new FileInfo(localPath).Length;
                if (localSize == entry.SizeBytes)
                {
                    _logger.LogDebug("Arquivo de retorno já existe localmente com mesmo tamanho, apagando remoto: '{File}'.", entry.Name);
                    try { client.DeleteFile(remotePath); } catch { }
                    continue;
                }
            }

            var dtInicio = DateTime.UtcNow;
            try
            {
                await transport.DownloadFileAsync(remotePath, localPath, ct);

                var tamanho = new FileInfo(localPath).Length;

                try
                {
                    await _logSftpRepository.InserirAsync(new LogSftp
                    {
                        CnConexaoSftp = conexaoRetorno.CnConexaoSftp,
                        IdTipo = "DOWNLOAD",
                        IdStatus = "S",
                        NmArquivo = entry.Name,
                        NrTamanhoBytes = tamanho,
                        NrDuracaoMs = (int)(DateTime.UtcNow - dtInicio).TotalMilliseconds,
                        DsMensagem = $"{conexaoRetorno.DsHost}:{conexaoRetorno.NrPorta}{remotePath}",
                        DtEvento = DateTime.UtcNow
                    }, ct);
                }
                catch { }

                try
                {
                    client.DeleteFile(remotePath);
                }
                catch (Exception exDel)
                {
                    if (isUltimoHorario)
                        _logger.LogError(exDel, "Falha ao apagar arquivo remoto (última execução do dia): '{Path}'. Verifique permissão de exclusão no SFTP.", remotePath);
                    else
                        _logger.LogWarning(exDel, "Falha ao apagar arquivo remoto após download: '{Path}'. Será tentado na próxima execução.", remotePath);
                }

                succeeded++;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex.GetType().Name.Contains("Ssh") && ct.IsCancellationRequested)
            {
                throw new OperationCanceledException("Download cancelado durante operação SSH.", ex, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao baixar retorno '{File}' de '{Host}'.", entry.Name, conexaoRetorno.DsHost);
                errors.Add($"{entry.Name}: falha no download");
                failed++;

                try
                {
                    await _logSftpRepository.InserirAsync(new LogSftp
                    {
                        CnConexaoSftp = conexaoRetorno.CnConexaoSftp,
                        IdTipo = "DOWNLOAD",
                        IdStatus = "E",
                        NmArquivo = entry.Name,
                        DsMensagem = "Falha no download",
                        DtEvento = DateTime.UtcNow
                    }, ct);
                }
                catch { }
            }
        }

        return new FileTransferResult(succeeded + failed, succeeded, failed, errors);
    }
}
