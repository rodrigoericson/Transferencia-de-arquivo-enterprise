using Microsoft.Extensions.Logging;
using STA.Worker.Models;

namespace STA.Worker.Services;

public record FileTransferResult(
    int FilesProcessed,
    int FilesSucceeded,
    int FilesFailed,
    List<string> ErrorMessages);

public interface IFileTransferService
{
    Task<FileTransferResult> TransferAsync(
        TransferPath config,
        string sourceDirectory,
        string destinationDirectory,
        bool overwriteExisting,
        int timeoutCompactacaoMs,
        CancellationToken cancellationToken);
}

public class FileTransferService : IFileTransferService
{
    private readonly IFileMaskMatcher _maskMatcher;
    private readonly IFileSizeValidator _sizeValidator;
    private readonly IFileLockChecker _lockChecker;
    private readonly IFileCompressor _compressor;
    private readonly ILogger<FileTransferService> _logger;

    public FileTransferService(
        IFileMaskMatcher maskMatcher,
        IFileSizeValidator sizeValidator,
        IFileLockChecker lockChecker,
        IFileCompressor compressor,
        ILogger<FileTransferService> logger)
    {
        _maskMatcher = maskMatcher;
        _sizeValidator = sizeValidator;
        _lockChecker = lockChecker;
        _compressor = compressor;
        _logger = logger;
    }

    public async Task<FileTransferResult> TransferAsync(
        TransferPath config,
        string sourceDirectory,
        string destinationDirectory,
        bool overwriteExisting,
        int timeoutCompactacaoMs,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        int processed = 0, succeeded = 0, failed = 0;

        if (!Directory.Exists(sourceDirectory))
        {
            _logger.LogWarning("Diretório de origem não existe: '{Path}'.", sourceDirectory);
            return new FileTransferResult(0, 0, 0, new List<string> { $"Diretório de origem não existe: {sourceDirectory}" });
        }

        Directory.CreateDirectory(destinationDirectory);

        if (!string.IsNullOrWhiteSpace(config.DiretorioBackup))
            Directory.CreateDirectory(config.DiretorioBackup);

        var files = new DirectoryInfo(sourceDirectory).GetFiles();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                processed++;

                if (!File.Exists(file.FullName))
                    continue;

                if (!_maskMatcher.Match(file.Name, config.MascaraArq))
                    continue;

                if (_lockChecker.IsFileLocked(file.FullName))
                {
                    _logger.LogWarning("Arquivo em uso, ignorado: '{File}'.", file.Name);
                    failed++;
                    errors.Add($"Arquivo em uso: {file.Name}");
                    continue;
                }

                if (!_sizeValidator.IsWithinRange(file.Length, config.TamanhoInicialArqBytes, config.TamanhoFinalArqBytes))
                {
                    _logger.LogDebug("Arquivo fora da faixa de tamanho: '{File}' ({Size} bytes).", file.Name, file.Length);
                    continue;
                }

                var fileToTransfer = file.FullName;
                var fileNameToTransfer = file.Name;

                // Compressão na origem
                if (_compressor.IsCompressionTypeSupported(config.CompactaOrigemTipo)
                    && !_compressor.IsFileCompressed(file.FullName))
                {
                    var archiveName = file.Name + "." + config.CompactaOrigemTipo.ToLowerInvariant();
                    var archivePath = Path.Combine(sourceDirectory, archiveName);

                    var compressed = await _compressor.CompressAsync(
                        file.FullName, archivePath, config.CompactaOrigemTipo, timeoutCompactacaoMs, cancellationToken);

                    if (compressed)
                    {
                        fileToTransfer = archivePath;
                        fileNameToTransfer = archiveName;
                    }
                    else
                    {
                        _logger.LogWarning("Falha na compactação de '{File}'. Transferindo original.", file.Name);
                    }
                }

                // Cópia para destino
                var destinationPath = Path.Combine(destinationDirectory, fileNameToTransfer);
                File.Copy(fileToTransfer, destinationPath, overwriteExisting);

                // Backup
                if (!string.IsNullOrWhiteSpace(config.DiretorioBackup))
                {
                    try
                    {
                        var backupPath = Path.Combine(config.DiretorioBackup, fileNameToTransfer);
                        File.Copy(fileToTransfer, backupPath, overwriteExisting);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Falha ao criar backup de '{File}'.", fileNameToTransfer);
                    }
                }

                // Descompactação no destino
                if (!string.IsNullOrWhiteSpace(config.DescompactaDestino)
                    && config.DescompactaDestino.Equals("SIM", StringComparison.OrdinalIgnoreCase)
                    && _compressor.IsFileCompressed(destinationPath))
                {
                    var decompressed = await _compressor.DecompressAsync(
                        destinationPath, destinationDirectory, timeoutCompactacaoMs, cancellationToken);

                    if (decompressed)
                        File.Delete(destinationPath);
                    else
                        _logger.LogWarning("Falha na descompactação de '{File}' no destino.", fileNameToTransfer);
                }

                // Exclusão da origem
                if (fileToTransfer != file.FullName && File.Exists(fileToTransfer))
                    File.Delete(fileToTransfer);

                if (File.Exists(file.FullName))
                    File.Delete(file.FullName);

                succeeded++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                errors.Add($"{file.Name}: {ex.Message}");
                _logger.LogError(ex, "Erro ao transferir '{File}'.", file.Name);
            }
        }

        // Expurgo de arquivos antigos no backup
        if (!string.IsNullOrWhiteSpace(config.DiretorioBackup) && config.DiasExcluir > 0)
            PurgeOldFiles(config.DiretorioBackup, config.DiasExcluir, config.MascaraArq);

        return new FileTransferResult(processed, succeeded, failed, errors);
    }

    private void PurgeOldFiles(string directory, int retentionDays, string mask)
    {
        try
        {
            if (!Directory.Exists(directory))
                return;

            var cutoff = DateTime.Now.AddDays(-retentionDays);
            var files = new DirectoryInfo(directory).GetFiles();

            foreach (var file in files)
            {
                if (file.LastWriteTime < cutoff && _maskMatcher.Match(file.Name, mask))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Falha ao excluir arquivo antigo '{File}'.", file.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao purgar arquivos antigos de '{Dir}'.", directory);
        }
    }
}
