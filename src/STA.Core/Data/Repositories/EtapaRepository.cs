using Microsoft.EntityFrameworkCore;
using STA.Core.Data.Entities;

namespace STA.Core.Data.Repositories;

public class EtapaRepository : IEtapaRepository
{
    private readonly StaDbContext _context;

    public EtapaRepository(StaDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EtapaTransferencia>> BuscarEtapasAtivasAsync(
        int cnSistema, CancellationToken cancellationToken = default)
    {
        return await _context.Etapas
            .Where(e => e.CnSistema == cnSistema && e.FlAtivo)
            .Include(e => e.Rotas.Where(r => r.FlAtivo).OrderBy(r => r.NrOrdem))
                .ThenInclude(r => r.Destinos.Where(d => d.FlAtivo).OrderBy(d => d.NrOrdem))
            .OrderBy(e => e.NrOrdemExecucao)
            .ToListAsync(cancellationToken);
    }
}
