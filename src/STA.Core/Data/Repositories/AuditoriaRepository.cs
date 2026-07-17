using Microsoft.EntityFrameworkCore;
using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly StaDbContext _context;

    public AuditoriaRepository(StaDbContext context)
    {
        _context = context;
    }

    public async Task InserirAsync(Auditoria auditoria, CancellationToken ct = default)
    {
        _context.Auditorias.Add(auditoria);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<Auditoria> items, int total)> ListarAsync(
        string? usuario = null,
        string? entidade = null,
        string? acao = null,
        DateTime? de = null,
        DateTime? ate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _context.Auditorias.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(usuario))
            query = query.Where(a => a.NmUsuario.Contains(usuario));

        if (!string.IsNullOrWhiteSpace(entidade))
            query = query.Where(a => a.IdEntidade == entidade);

        if (!string.IsNullOrWhiteSpace(acao))
            query = query.Where(a => a.IdAcao == acao);

        if (de.HasValue)
        {
            var deUtc = DateTime.SpecifyKind(de.Value, DateTimeKind.Utc);
            query = query.Where(a => a.DtAcao >= deUtc);
        }

        if (ate.HasValue)
        {
            var ateUtc = DateTime.SpecifyKind(ate.Value, DateTimeKind.Utc).AddDays(1);
            query = query.Where(a => a.DtAcao < ateUtc);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.DtAcao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
