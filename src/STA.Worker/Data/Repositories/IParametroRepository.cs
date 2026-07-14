namespace STA.Worker.Data.Repositories;

/// <summary>
/// Contrato para acesso a parâmetros do sistema (horários e período de execução).
/// Contrato para acesso a parâmetros de execução do sistema via PostgreSQL.
/// </summary>
public interface IParametroRepository
{
    /// <summary>
    /// Busca os parâmetros de execução do sistema: hora inicial, hora final e período em minutos.
    /// Retorna null se o sistema não existir ou os parâmetros não estiverem configurados.
    /// </summary>
    /// <param name="aliasSistema">Alias do sistema (e.g., "STA").</param>
    /// <param name="codHoraIni">Código do parâmetro de hora inicial (e.g., 1).</param>
    /// <param name="codHoraFim">Código do parâmetro de hora final (e.g., 2).</param>
    /// <param name="codPeriodo">Código do parâmetro de período (e.g., 3).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<ParametrosExecucao?> BuscarParametrosExecucaoAsync(
        string aliasSistema,
        int codHoraIni,
        int codHoraFim,
        int codPeriodo,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado da busca de parâmetros de execução do sistema.
/// Equivale ao que antes era setado em Module1 (horaIniSistema, horaFimSistema, PeriodoSistemaMin).
/// </summary>
public record ParametrosExecucao(
    string HoraInicial,
    string HoraFinal,
    int PeriodoMinutos);
