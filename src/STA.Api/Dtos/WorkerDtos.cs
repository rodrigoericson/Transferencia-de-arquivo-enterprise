namespace STA.Api.Dtos;

public record WorkerStatusDto(
    string Status,
    DateTime? UltimoCiclo,
    string? UltimoCicloStatus,
    int ArquivosHoje,
    int ErrosHoje);

public record ExecucaoDto(
    bool Executando,
    bool Pausado,
    string? EtapaAtual,
    DateTime? CicloIniciadoEm,
    DateTime? UltimoCicloFim,
    DateTime? ProximoCicloEm,
    long? DuracaoUltimoCicloMs);
