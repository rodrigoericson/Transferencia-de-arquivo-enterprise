using Microsoft.Extensions.Logging;
using STA.Worker.Data.Repositories;

namespace STA.Worker.Services;

public interface IFileRetentionService
{
    Task<int> CleanupOldLogsAsync(
        string aliasSistema,
        int cnProcesso,
        int retentionDays,
        CancellationToken cancellationToken);
}

public class FileRetentionService : IFileRetentionService
{
    private readonly ILogRepository _logRepository;
    private readonly ILogger<FileRetentionService> _logger;

    public FileRetentionService(ILogRepository logRepository, ILogger<FileRetentionService> logger)
    {
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<int> CleanupOldLogsAsync(
        string aliasSistema,
        int cnProcesso,
        int retentionDays,
        CancellationToken cancellationToken)
    {
        if (retentionDays <= 0)
            return 0;

        try
        {
            var excluidos = await _logRepository.ExcluirLogsAntigosAsync(
                aliasSistema, cnProcesso, retentionDays, cancellationToken);

            if (excluidos > 0)
                _logger.LogInformation("Excluídos {Count} logs antigos (retenção: {Dias} dias).", excluidos, retentionDays);

            return excluidos;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao excluir logs antigos.");
            return 0;
        }
    }
}
