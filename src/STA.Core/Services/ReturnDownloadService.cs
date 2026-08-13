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
    private readonly ILogger<ReturnDownloadService> _logger;

    public ReturnDownloadService(
        IFileMaskMatcher maskMatcher,
        IFileLockChecker lockChecker,
        ILogSftpRepository logSftpRepository,
        ILogger<ReturnDownloadService> logger)
    {
        _maskMatcher = maskMatcher;
        _lockChecker = lockChecker;
        _logSftpRepository = logSftpRepository;
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
        if (!IsRetornoHabilitado(config))
            return new FileTransferResult(0, 0, 0, []);

        if (!SftpPathValidator.TryNormalize(config.DsDiretorioRetorno, out var normalizedRetornoDir, out var erroPath))
        {
            _logger.LogWarning("Diretório de retorno inválido: {Erro}", erroPath);
            return new FileTransferResult(0, 0, 0, [$"Configuração inválida: {erroPath}"]);
        }

        if (!TryPrepareLocalDirectory(config.DsDiretorioLocalRetorno))
            return new FileTransferResult(0, 0, 0, [$"Erro ao criar diretório local de retorno."]);

        ISftpClientWrapper client;
        try { client = pool.GetOrCreate(conexaoRetorno); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao conectar SFTP de retorno '{Nome}'.", conexaoRetorno.NmConexao);
            return new FileTransferResult(0, 0, 0, [$"Falha ao conectar SFTP retorno."]);
        }

        var transport = new SftpTransport(client, _logger as ILogger<SftpTransport> ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SftpTransport>.Instance);

        var entries = await ListRemoteFilesAsync(client, normalizedRetornoDir, config);
        if (entries == null)
            return new FileTransferResult(0, 0, 0, [$"Erro ao listar diretório de retorno."]);

        var errors = new List<string>();
        int succeeded = 0, failed = 0;

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();

            var (fileSucceeded, fileError) = await ProcessReturnFileAsync(
                entry, normalizedRetornoDir, config, conexaoRetorno, transport, client, isUltimoHorario, ct);

            if (fileSucceeded == true)
                succeeded++;
            else if (fileSucceeded == false)
            {
                failed++;
                if (fileError != null)
                    errors.Add(fileError);
            }
        }

        return new FileTransferResult(succeeded + failed, succeeded, failed, errors);
    }

    private static bool IsRetornoHabilitado(TransferPath config)
        => config.FlHabilitarRetorno
            && !string.IsNullOrWhiteSpace(config.DsDiretorioRetorno)
            && !string.IsNullOrWhiteSpace(config.DsDiretorioLocalRetorno);

    private bool TryPrepareLocalDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Caminho de diretório local de retorno está vazio.");
            return false;
        }

        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível criar diretório local de retorno: '{Path}'.", path);
            return false;
        }
    }

    private async Task<List<SftpRemoteEntry>?> ListRemoteFilesAsync(ISftpClientWrapper client, string directory, TransferPath config)
    {
        try
        {
            return client.ListDirectoryDetailed(directory)
                .Where(e => !e.IsDirectory && _maskMatcher.Match(e.Name, config.DsMascaraRetorno))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao listar diretório de retorno '{Dir}'.", directory);
            return null;
        }
    }

    private async Task<(bool? Succeeded, string? Error)> ProcessReturnFileAsync(
        SftpRemoteEntry entry,
        string normalizedRetornoDir,
        TransferPath config,
        ConexaoSftp conexaoRetorno,
        SftpTransport transport,
        ISftpClientWrapper client,
        bool isUltimoHorario,
        CancellationToken ct)
    {
        if (!IsFileNameSafe(entry.Name))
        {
            _logger.LogWarning("Nome de arquivo remoto inseguro ignorado: '{Name}'.", entry.Name);
            return (null, null);
        }

        var remotePath = $"{normalizedRetornoDir.TrimEnd('/')}/{entry.Name}";
        if (string.IsNullOrWhiteSpace(config.DsDiretorioLocalRetorno))
            return (false, "Diretório local de retorno não configurado");

        var localPath = Path.Combine(config.DsDiretorioLocalRetorno, entry.Name);

        if (IsLocalFileLocked(localPath))
        {
            _logger.LogDebug("Arquivo local de retorno em uso, será tentado no próximo ciclo: '{File}'.", entry.Name);
            return (null, null);
        }

        if (await CheckIfAlreadyDownloadedAsync(localPath, entry.SizeBytes, remotePath, client))
            return (null, null);

        var dtInicio = DateTime.UtcNow;
        try
        {
            await transport.DownloadFileAsync(remotePath, localPath, ct);
            var tamanho = new FileInfo(localPath).Length;

            await LogSuccessfulDownloadAsync(conexaoRetorno, entry.Name, remotePath, tamanho, dtInicio, ct);
            await TryDeleteRemoteFileAsync(client, remotePath, isUltimoHorario);

            return (true, null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex.GetType().Name.Contains("Ssh") && ct.IsCancellationRequested)
        {
            throw new OperationCanceledException("Download cancelado durante operação SSH.", ex, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao baixar retorno '{File}' de '{Host}'.", entry.Name, conexaoRetorno.DsHost);
            await LogFailedDownloadAsync(conexaoRetorno, entry.Name, ct);
            return (false, $"{entry.Name}: falha no download");
        }
    }

    private static bool IsFileNameSafe(string fileName)
        => PathSafety.IsFileNameSafe(fileName);

    private bool IsLocalFileLocked(string path)
        => File.Exists(path) && _lockChecker.IsFileLocked(path);

    private async Task<bool> CheckIfAlreadyDownloadedAsync(string localPath, long remoteSize, string remotePath, ISftpClientWrapper client)
    {
        if (!File.Exists(localPath))
            return false;

        var localSize = new FileInfo(localPath).Length;
        if (localSize != remoteSize)
            return false;

        _logger.LogDebug("Arquivo de retorno já existe localmente com mesmo tamanho, apagando remoto: '{File}'.", Path.GetFileName(localPath));
        try { client.DeleteFile(remotePath); } catch { }
        return true;
    }

    private async Task LogSuccessfulDownloadAsync(ConexaoSftp conexao, string fileName, string remotePath, long tamanho, DateTime inicio, CancellationToken ct)
    {
        try
        {
            await _logSftpRepository.InserirAsync(new LogSftp
            {
                CnConexaoSftp = conexao.CnConexaoSftp,
                IdTipo = "DOWNLOAD",
                IdStatus = "S",
                NmArquivo = fileName,
                NrTamanhoBytes = tamanho,
                NrDuracaoMs = (int)(DateTime.UtcNow - inicio).TotalMilliseconds,
                DsMensagem = $"{conexao.DsHost}:{conexao.NrPorta}{remotePath}",
                DtEvento = DateTime.UtcNow
            }, ct);
        }
        catch { }
    }

    private async Task TryDeleteRemoteFileAsync(ISftpClientWrapper client, string remotePath, bool isUltimoHorario)
    {
        try { client.DeleteFile(remotePath); }
        catch (Exception ex)
        {
            if (isUltimoHorario)
                _logger.LogError(ex, "Falha ao apagar arquivo remoto (última execução do dia): '{Path}'. Verifique permissão de exclusão no SFTP.", remotePath);
            else
                _logger.LogWarning(ex, "Falha ao apagar arquivo remoto após download: '{Path}'. Será tentado na próxima execução.", remotePath);
        }
    }

    private async Task LogFailedDownloadAsync(ConexaoSftp conexao, string fileName, CancellationToken ct)
    {
        try
        {
            await _logSftpRepository.InserirAsync(new LogSftp
            {
                CnConexaoSftp = conexao.CnConexaoSftp,
                IdTipo = "DOWNLOAD",
                IdStatus = "E",
                NmArquivo = fileName,
                DsMensagem = "Falha no download",
                DtEvento = DateTime.UtcNow
            }, ct);
        }
        catch { }
    }
}
