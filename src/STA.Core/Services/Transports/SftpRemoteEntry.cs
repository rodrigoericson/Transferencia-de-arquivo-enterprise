namespace STA.Core.Services.Transports;

public record SftpRemoteEntry(
    string Name,
    string FullPath,
    bool IsDirectory,
    long SizeBytes,
    DateTime LastModifiedUtc);
