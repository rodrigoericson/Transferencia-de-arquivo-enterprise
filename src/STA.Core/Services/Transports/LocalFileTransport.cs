using Microsoft.Extensions.Logging;

namespace STA.Core.Services.Transports;

public class LocalFileTransport : IDestinationTransport
{
    private readonly bool _overwriteDefault;

    public LocalFileTransport(bool overwriteDefault, ILogger<LocalFileTransport> logger)
    {
        _overwriteDefault = overwriteDefault;
    }

    public Task UploadFileAsync(string sourceFilePath, string remotePath, bool overwrite, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(remotePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.Copy(sourceFilePath, remotePath, overwrite || _overwriteDefault);
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        if (File.Exists(remotePath))
            File.Delete(remotePath);
        return Task.CompletedTask;
    }

    public Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<string>> ListFilesAsync(string remoteDirectory, CancellationToken ct = default)
    {
        if (!Directory.Exists(remoteDirectory))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var files = Directory.GetFiles(remoteDirectory)
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(files);
    }
}
