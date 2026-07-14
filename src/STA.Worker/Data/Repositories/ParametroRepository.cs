using Microsoft.EntityFrameworkCore;

namespace STA.Worker.Data.Repositories;

/// <summary>
/// Acesso a parâmetros de configuração do sistema via EF Core.
/// Acesso a parâmetros de execução do sistema via PostgreSQL.
/// </summary>
public class ParametroRepository : IParametroRepository
{
    private readonly StaDbContext _context;

    public ParametroRepository(StaDbContext context)
    {
        _context = context;
    }

    public async Task<ParametrosExecucao?> BuscarParametrosExecucaoAsync(
        string aliasSistema,
        int codHoraIni,
        int codHoraFim,
        int codPeriodo,
        CancellationToken cancellationToken = default)
    {
        // Busca os 3 parâmetros em uma única query com JOIN implícito via navigation property.
        // No legado, isso era feito com SELECT + concatenação (propenso a SQLi).
        var parametros = await _context.Parametros
            .Where(p => p.Sistema!.CdAliasSistema == aliasSistema
                && (p.CnParametroSistema == codHoraIni
                    || p.CnParametroSistema == codHoraFim
                    || p.CnParametroSistema == codPeriodo))
            .OrderBy(p => p.CnParametroSistema)
            .Select(p => new { p.CnParametroSistema, p.CdParametroSistema })
            .ToListAsync(cancellationToken);

        // Espera exatamente 3 parâmetros. Qualquer outro número indica configuração incompleta.
        if (parametros.Count != 3)
            return null;

        var horaIni = parametros.First(p => p.CnParametroSistema == codHoraIni).CdParametroSistema;
        var horaFim = parametros.First(p => p.CnParametroSistema == codHoraFim).CdParametroSistema;
        var periodoStr = parametros.First(p => p.CnParametroSistema == codPeriodo).CdParametroSistema;

        if (!int.TryParse(periodoStr, out var periodo) || periodo <= 0)
            return null;

        return new ParametrosExecucao(horaIni, horaFim, periodo);
    }
}
