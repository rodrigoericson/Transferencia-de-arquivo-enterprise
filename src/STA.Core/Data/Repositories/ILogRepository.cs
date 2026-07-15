namespace STA.Core.Data.Repositories;

/// <summary>
/// Contrato para persistência de logs de processo via PostgreSQL.
/// </summary>
public interface ILogRepository
{
    /// <summary>
    /// Insere um registro de log de processo chamando a function PostgreSQL fn_inclui_log_processo.
    /// </summary>
    /// <param name="aliasSistema">Alias do sistema (e.g., "STA").</param>
    /// <param name="cnProcesso">Número do processo.</param>
    /// <param name="dtInicio">Data/hora de início da operação.</param>
    /// <param name="status">Status: 'R' (rodando), 'O' (sucesso), 'W' (aviso), 'E' (erro).</param>
    /// <param name="qtRegistrosProcessados">Quantidade de registros processados.</param>
    /// <param name="vlRegistrosProcessados">Valor de registros processados.</param>
    /// <param name="qtRegistrosErro">Quantidade de registros com erro.</param>
    /// <param name="vlRegistrosErro">Valor de registros com erro.</param>
    /// <param name="xmlObservacao">Observações em formato XML.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>ID do log inserido (cn_log_processo), ou null se falhar.</returns>
    Task<int?> InserirLogAsync(
        string aliasSistema,
        int cnProcesso,
        DateTime dtInicio,
        string status,
        long qtRegistrosProcessados,
        long vlRegistrosProcessados,
        long qtRegistrosErro,
        long vlRegistrosErro,
        string xmlObservacao,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exclui logs de processo com mais de N dias via DELETE parametrizado no PostgreSQL (dt_fim_processo < hoje - dias).
    /// </summary>
    /// <param name="aliasSistema">Alias do sistema.</param>
    /// <param name="cnProcesso">Número do processo.</param>
    /// <param name="diasManter">Quantidade de dias para manter (logs mais velhos serão excluídos).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Quantidade de registros excluídos.</returns>
    Task<int> ExcluirLogsAntigosAsync(
        string aliasSistema,
        int cnProcesso,
        int diasManter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza a data/hora de fim e status de um log de processo aberto.
    /// Usado para fechar um log que foi aberto com status 'R' (rodando).
    /// </summary>
    /// <param name="cnLogProcesso">ID do log de processo a atualizar.</param>
    /// <param name="dtFim">Data/hora de término da operação.</param>
    /// <param name="statusFinal">Status final: 'O' (sucesso), 'W' (aviso), 'E' (erro).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Número de registros atualizados (0 ou 1).</returns>
    Task<int> AtualizarFimAsync(
        int cnLogProcesso,
        DateTime dtFim,
        string statusFinal,
        CancellationToken cancellationToken = default);
}
