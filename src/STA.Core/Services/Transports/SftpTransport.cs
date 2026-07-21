using Microsoft.Extensions.Logging;

namespace STA.Core.Services.Transports;

public class SftpTransport : IDestinationTransport
{
    private readonly ISftpClientWrapper _client;
    private readonly ILogger<SftpTransport> _logger;
    private const int RetryDelayMs = 5000;

    public SftpTransport(ISftpClientWrapper client, ILogger<SftpTransport> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task UploadFileAsync(string sourceFilePath, string remotePath, bool overwrite, CancellationToken ct = default)
    {
        EnsureConnected();

        var tmpPath = remotePath + ".tmp";

        try
        {
            await UploadWithRetryAsync(sourceFilePath, tmpPath, ct);

            if (overwrite && _client.Exists(remotePath))
                _client.DeleteFile(remotePath);

            _client.RenameFile(tmpPath, remotePath);
        }
        catch
        {
            try
            {
                if (_client.Exists(tmpPath))
                    _client.DeleteFile(tmpPath);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx,
                    "Falha ao limpar arquivo temporario SFTP: {Path}", tmpPath);
            }
            throw;
        }
    }

    public Task DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        EnsureConnected();

        if (_client.Exists(remotePath))
            _client.DeleteFile(remotePath);

        return Task.CompletedTask;
    }

    public Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_client.IsConnected)
                _client.Connect();
            return Task.FromResult(_client.IsConnected);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao testar conexao SFTP.");
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(string remoteDirectory, CancellationToken ct = default)
    {
        EnsureConnected();

        try
        {
            var files = _client.ListDirectory(remoteDirectory).ToList();
            return Task.FromResult<IReadOnlyList<string>>(files);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao listar diretorio SFTP: {Dir}", remoteDirectory);
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }
    }

    private async Task UploadWithRetryAsync(string sourceFilePath, string remotePath, CancellationToken ct)
    {
        try
        {
            DoUpload(sourceFilePath, remotePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Upload SFTP falhou, tentando retry em {Delay}ms...", RetryDelayMs);
            await Task.Delay(RetryDelayMs, ct);
            DoUpload(sourceFilePath, remotePath);
        }
    }

    private void DoUpload(string sourceFilePath, string remotePath)
    {
        using var stream = File.OpenRead(sourceFilePath);
        _client.UploadFile(stream, remotePath);
    }

    private void EnsureConnected()
    {
        if (!_client.IsConnected)
            _client.Connect();
    }
}
