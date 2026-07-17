using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public interface IAuditoriaRepository
{
    Task InserirAsync(Auditoria auditoria, CancellationToken ct = default);

    Task<(IReadOnlyList<Auditoria> items, int total)> ListarAsync(
        string? usuario = null,
        string? entidade = null,
        string? acao = null,
        DateTime? de = null,
        DateTime? ate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
}
