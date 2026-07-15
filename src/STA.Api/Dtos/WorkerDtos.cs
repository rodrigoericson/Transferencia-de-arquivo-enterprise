namespace STA.Api.Dtos;

public record WorkerStatusDto(
    string Status,
    DateTime? UltimoCiclo,
    string? UltimoCicloStatus,
    int ArquivosHoje,
    int ErrosHoje);
