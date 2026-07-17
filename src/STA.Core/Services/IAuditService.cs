namespace STA.Core.Services;

public interface IAuditService
{
    Task RegistrarAsync(
        string idEntidade,
        int idReferencia,
        string idAcao,
        string? detalhe = null,
        CancellationToken ct = default);
}
