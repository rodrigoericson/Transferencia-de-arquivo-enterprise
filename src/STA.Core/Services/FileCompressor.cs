using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace STA.Core.Services;

public interface IFileCompressor
{
    Task<bool> CompressAsync(string sourceFilePath, string outputArchivePath, string compressionType, int timeoutMs, CancellationToken cancellationToken);
    Task<bool> DecompressAsync(string archiveFilePath, string outputDirectory, int timeoutMs, CancellationToken cancellationToken);
    bool IsCompressionTypeSupported(string type);
    bool IsFileCompressed(string filePath);
}

public class FileCompressor : IFileCompressor
{
    private static readonly HashSet<string> CompressedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".7z", ".zip", ".rar", ".gz", ".bz2", ".xz"
    };

    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "7Z", "ZIP"
    };

    private readonly string _sevenZipPath;
    private readonly ILogger<FileCompressor> _logger;

    public FileCompressor(string sevenZipPath, ILogger<FileCompressor> logger)
    {
        _sevenZipPath = sevenZipPath;
        _logger = logger;
    }

    public bool IsCompressionTypeSupported(string type)
        => !string.IsNullOrWhiteSpace(type) && SupportedTypes.Contains(type);

    public bool IsFileCompressed(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return !string.IsNullOrEmpty(ext) && CompressedExtensions.Contains(ext);
    }

    public async Task<bool> CompressAsync(
        string sourceFilePath,
        string outputArchivePath,
        string compressionType,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (!IsCompressionTypeSupported(compressionType))
        {
            _logger.LogWarning("Tipo de compressão não suportado: '{Type}'.", compressionType);
            return false;
        }

        if (!File.Exists(_sevenZipPath))
        {
            _logger.LogWarning("7-Zip não encontrado em '{Path}'.", _sevenZipPath);
            return false;
        }

        // -mx=3 = Fast (5-10x mais rápido que Ultra, ainda com boa compressão para envio em rede)
        // Objetivo é enviar, não arquivar — não precisa do nível máximo
        var args = new[] { "a", $"-t{compressionType.ToLowerInvariant()}", "-mx=3", outputArchivePath, sourceFilePath };
        return await RunProcessAsync(args, timeoutMs, cancellationToken);
    }

    public async Task<bool> DecompressAsync(
        string archiveFilePath,
        string outputDirectory,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(_sevenZipPath))
        {
            _logger.LogWarning("7-Zip não encontrado em '{Path}'.", _sevenZipPath);
            return false;
        }

        var args = new[] { "x", archiveFilePath, $"-o{outputDirectory}", "-y" };
        return await RunProcessAsync(args, timeoutMs, cancellationToken);
    }

    private async Task<bool> RunProcessAsync(string[] args, int timeoutMs, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _sevenZipPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        try
        {
            process.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            await process.WaitForExitAsync(cts.Token);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogWarning("7-Zip retornou código {ExitCode}. Stderr: {Error}", process.ExitCode, stderr);
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                _logger.LogWarning("7-Zip cancelado por timeout ({Timeout}ms) ou cancelamento.", timeoutMs);
            }
            return false;
        }
    }
}
