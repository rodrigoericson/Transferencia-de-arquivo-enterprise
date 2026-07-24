using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using STA.Core.Data.Repositories;
using STA.Core.Services;
using STA.Core.Settings;

namespace STA.Worker;

public class Worker : BackgroundService
{
    private const int COD_HORA_INI = 1;
    private const int COD_HORA_FIM = 2;
    private const int COD_PERIODO = 3;

    private readonly ILogger<Worker> _logger;
    private readonly IOptions<StaSettings> _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EstadoExecucao _estado;
    private readonly string _aliasSistema;

    private TimeSpan _interval = TimeSpan.FromMinutes(5);
    private ParametrosExecucao? _ultimosParametros;

    public Worker(
        ILogger<Worker> logger,
        IOptions<StaSettings> settings,
        IServiceScopeFactory scopeFactory,
        EstadoExecucao estado)
    {
        _logger = logger;
        _settings = settings;
        _scopeFactory = scopeFactory;
        _estado = estado;
        _aliasSistema = settings.Value.NomeSistema;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("STA Worker iniciado em: {Time}", DateTimeOffset.Now);

        try
        {
            await LimparLogsOrfaosAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // Se pausado, checa a cada 5 segundos se reativou (resposta rápida ao resume)
                if (await IsWorkerPausedAsync(stoppingToken))
                {
                    _logger.LogDebug("Worker pausado. Aguardando retomada...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                try
                {
                    await ExecutarCicloAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Erro no ciclo de execução. Próxima tentativa em {Interval}.", _interval);
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("STA Worker encerrado em: {Time}", DateTimeOffset.Now);
    }

    private async Task ExecutarCicloAsync(CancellationToken stoppingToken)
    {
        if (await IsWorkerPausedAsync(stoppingToken))
        {
            _estado.SetPausado(true, DateTime.UtcNow.Add(_interval));
            _logger.LogDebug("Worker pausado via banco. Ciclo ignorado.");
            return;
        }
        _estado.SetPausado(false);

        await AtualizarParametrosAsync(stoppingToken);

        if (_ultimosParametros is null)
        {
            _logger.LogWarning("Parâmetros de execução não configurados para '{Sistema}'. Ciclo ignorado.", _aliasSistema);
            return;
        }

        if (!TimeSpan.TryParse(_ultimosParametros.HoraInicial, out var horaIni)
            || !TimeSpan.TryParse(_ultimosParametros.HoraFinal, out var horaFim))
        {
            _logger.LogWarning(
                "Formato de horário inválido (Ini='{Ini}', Fim='{Fim}'). Ciclo ignorado.",
                _ultimosParametros.HoraInicial, _ultimosParametros.HoraFinal);
            return;
        }

        var agora = DateTime.Now.TimeOfDay;
        if (!PeriodoExecucaoCalculator.DentroPeriodo(horaIni, horaFim, agora))
        {
            _logger.LogDebug(
                "Fora da janela de execução ({Ini}–{Fim}). Ciclo ignorado.",
                _ultimosParametros.HoraInicial, _ultimosParametros.HoraFinal);
            return;
        }

        _logger.LogInformation(
            "Ciclo de execução dentro da janela ({Ini}–{Fim}) em: {Time}.",
            _ultimosParametros.HoraInicial, _ultimosParametros.HoraFinal, DateTimeOffset.Now);

        await ExecutarTransferenciasAsync(stoppingToken);
        await ExecutarLimpezaLogsAsync(stoppingToken);
    }

    private async Task ExecutarTransferenciasAsync(CancellationToken stoppingToken)
    {
        var settings = _settings.Value;

        using var scope = _scopeFactory.CreateScope();
        var etapaProvider = scope.ServiceProvider.GetRequiredService<IEtapaConfigProvider>();
        var pathLoader = scope.ServiceProvider.GetRequiredService<IPathConfigLoader>();
        var transferService = scope.ServiceProvider.GetRequiredService<IFileTransferService>();
        var purgeService = scope.ServiceProvider.GetRequiredService<IFilePurgeService>();

        // Pool SFTP: abre conexões no início do ciclo, fecha no fim
        var sftpPool = new STA.Core.Services.Transports.SftpConnectionPool(
            scope.ServiceProvider.GetRequiredService<STA.Core.Services.Transports.ISftpClientFactory>(),
            scope.ServiceProvider.GetRequiredService<STA.Core.Services.Transports.ICredencialProtector>(),
            scope.ServiceProvider.GetRequiredService<ILogger<STA.Core.Services.Transports.SftpConnectionPool>>(),
            scope.ServiceProvider.GetRequiredService<STA.Core.Data.Repositories.ILogSftpRepository>());
        if (transferService is STA.Core.Services.FileTransferService fts)
            fts.SetSftpPool(sftpPool);
        var returnDownloader = scope.ServiceProvider.GetRequiredService<STA.Core.Services.IReturnDownloadService>();
        var logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();

        var chains = await CarregarChainsAsync(etapaProvider, pathLoader, settings, logRepository, stoppingToken);
        if (chains is null)
            return;

        var dtInicio = DateTime.UtcNow;

        // Abrir log de ciclo com status 'R' (rodando) para ter o cnLogProcesso disponível
        int? cnLogProcesso = null;
        try
        {
            cnLogProcesso = await logRepository.InserirLogAsync(
                _aliasSistema, settings.CnProcesso, dtInicio, "R", 0, 0, 0, 0, string.Empty, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao abrir log de ciclo. Prosseguindo sem cn_log_processo.");
        }

        _estado.IniciarCiclo();

        var totals = new CicloTotals();
        bool cycleFailed = false;
        try
        {
            totals = await ProcessarChainsAsync(chains, transferService, purgeService, returnDownloader, sftpPool, settings, cnLogProcesso, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Erro durante processamento de chains.");
            cycleFailed = true;
        }
        finally
        {
            if (cycleFailed && totals.FilesFailed == 0)
                totals.ForceFailure();
            try { await FecharLogCicloAsync(logRepository, settings, dtInicio, totals, cnLogProcesso, CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao fechar log de ciclo."); }

            _estado.FinalizarCiclo(totals.FilesProcessed, DateTime.UtcNow.Add(_interval));

            try { await sftpPool.FlushLogsAsync(CancellationToken.None); }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao gravar logs SFTP do ciclo."); }
            finally
            {
                sftpPool.Dispose();
            }
        }
        ReportarResultado(totals);
    }

    private async Task<IReadOnlyList<STA.Core.Models.TransferChain>?> CarregarChainsAsync(
        IEtapaConfigProvider etapaProvider,
        IPathConfigLoader pathLoader,
        StaSettings settings,
        ILogRepository logRepository,
        CancellationToken stoppingToken)
    {
        if (settings.UseXmlFallback)
        {
            try
            {
                var xmlChains = pathLoader.CarregarCaminhos(settings.ArquivoPathsXml);
                _logger.LogDebug("UseXmlFallback ativo. Carregadas {Count} cadeias do XML.", xmlChains.Count);
                return xmlChains;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falha ao carregar configuração via XML.");
                await logRepository.InserirLogAsync(
                    _aliasSistema, settings.CnProcesso, DateTime.UtcNow, "E", 0, 0, 0, 0,
                    BuildLogObservacao("Leitura configuração (XML)", ex.Message), stoppingToken);
                return null;
            }
        }

        try
        {
            var systemId = await GetCnSistemaAsync(stoppingToken);
            if (systemId > 0)
            {
                var chains = await etapaProvider.CarregarEtapasAsync(systemId, stoppingToken);
                if (chains.Count > 0)
                {
                    _logger.LogDebug("Carregadas {Count} etapas do banco de dados.", chains.Count);
                    return chains;
                }
            }

            _logger.LogError("Nenhuma etapa configurada no banco para '{Sistema}'. Ciclo ignorado.", _aliasSistema);
            await logRepository.InserirLogAsync(
                _aliasSistema, settings.CnProcesso, DateTime.UtcNow, "E", 0, 0, 0, 0,
                BuildLogObservacao("Leitura configuração", "Nenhuma etapa ativa no banco."), stoppingToken);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao carregar etapas do banco.");
            await logRepository.InserirLogAsync(
                _aliasSistema, settings.CnProcesso, DateTime.UtcNow, "E", 0, 0, 0, 0,
                BuildLogObservacao("Leitura configuração (banco)", ex.Message), stoppingToken);
            return null;
        }
    }

    private async Task<int> GetCnSistemaAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<STA.Core.Data.StaDbContext>();
        var sistema = await context.Sistemas
            .Where(s => s.CdAliasSistema == _aliasSistema)
            .Select(s => s.CnSistema)
            .FirstOrDefaultAsync(stoppingToken);
        return sistema;
    }

    private async Task<CicloTotals> ProcessarChainsAsync(
        IReadOnlyList<STA.Core.Models.TransferChain> chains,
        IFileTransferService transferService,
        IFilePurgeService purgeService,
        STA.Core.Services.IReturnDownloadService returnDownloader,
        STA.Core.Services.Transports.SftpConnectionPool sftpPool,
        StaSettings settings,
        int? cnLogProcesso,
        CancellationToken stoppingToken)
    {
        var totals = new CicloTotals();

        foreach (var chain in chains)
        {
            _estado.SetEtapa(chain.Etapa);

            // Estrutura da chain: nó 0 = origem (com backup opcional), nós 1+ = destinos (fan-out)
            if (chain.Nodes.Count < 2) continue;

            var origem = chain.Nodes[0];

            // Buscar destinos com padrão de rename do banco
            var destinosTransfer = await BuscarDestinosComRenameAsync(origem.CnRota, chain.Nodes, stoppingToken);

            var result = await transferService.TransferFanOutAsync(
                origem,
                origem.DiretorioPrincipal,
                destinosTransfer,
                settings.SobreEscreverArquivos,
                settings.TimeoutCompactacaoMs,
                cnLogProcesso,
                stoppingToken);

            totals.Add(result);

            // Limpa backups antigos (purge) baseado no primeiro nó (que tem a info de DiasExcluir)
            purgeService.PurgeNode(origem);

            // Retorno SFTP: baixa arquivos do parceiro para pasta local
            if (origem.FlHabilitarRetorno && origem.CnConexaoSftpRetorno.HasValue)
            {
                try
                {
                    var conexaoRetorno = await BuscarConexaoSftpAsync(origem.CnConexaoSftpRetorno.Value, stoppingToken);
                    if (conexaoRetorno != null)
                    {
                        var retResult = await returnDownloader.ProcessarRetornoAsync(origem, conexaoRetorno, sftpPool, cnLogProcesso, stoppingToken);
                        totals.Add(retResult);
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao processar retorno SFTP da rota '{Rota}'.", origem.CnRota);
                }
            }
        }

        return totals;
    }

    private async Task FecharLogCicloAsync(
        ILogRepository logRepository,
        StaSettings settings,
        DateTime dtInicio,
        CicloTotals totals,
        int? cnLogProcesso,
        CancellationToken stoppingToken)
    {
        var status = totals.FilesFailed > 0 ? "W" : "O";

        if (!settings.GeraLogSucessoBancoDados && totals.FilesFailed == 0)
        {
            // Se não gera log de sucesso mas abriu o log, fecha com status 'O'
            if (cnLogProcesso.HasValue)
                await logRepository.AtualizarFimAsync(cnLogProcesso.Value, DateTime.UtcNow, status, stoppingToken);
            return;
        }

        if (cnLogProcesso.HasValue)
        {
            // Fecha o log aberto no início do ciclo
            await logRepository.AtualizarFimAsync(cnLogProcesso.Value, DateTime.UtcNow, status, stoppingToken);
        }
        else
        {
            // Fallback: inserir log completo (caso abertura tenha falhado)
            var obs = BuildLogObservacao(
                "Transferencia de arquivos",
                $"Processados: {totals.FilesProcessed}, Sucesso: {totals.FilesSucceeded}, Falhas: {totals.FilesFailed}");

            await logRepository.InserirLogAsync(
                _aliasSistema, settings.CnProcesso, dtInicio, status,
                totals.FilesSucceeded, 0, totals.FilesFailed, 0, obs, stoppingToken);
        }
    }

    private void ReportarResultado(CicloTotals totals)
    {
        if (totals.FilesFailed > 0)
            _logger.LogWarning(
                "Ciclo com falhas: {Processed} processados, {Succeeded} OK, {Failed} erros.",
                totals.FilesProcessed, totals.FilesSucceeded, totals.FilesFailed);
        else if (totals.FilesProcessed > 0)
            _logger.LogInformation(
                "Ciclo concluído: {Processed} arquivos transferidos com sucesso.", totals.FilesSucceeded);
    }

    private async Task ExecutarLimpezaLogsAsync(CancellationToken stoppingToken)
    {
        var settings = _settings.Value;
        if (settings.QtdDiasExcluirLog <= 0)
            return;

        using var scope = _scopeFactory.CreateScope();
        var retentionService = scope.ServiceProvider.GetRequiredService<IFileRetentionService>();

        await retentionService.CleanupOldLogsAsync(
            _aliasSistema, settings.CnProcesso, settings.QtdDiasExcluirLog, stoppingToken);
    }

    private static string BuildLogObservacao(string etapa, string mensagem)
    {
        var etapaEl = new System.Xml.Linq.XElement("Etapa", etapa);
        var obsEl = new System.Xml.Linq.XElement("Observacao", mensagem);
        return etapaEl + obsEl.ToString();
    }

    private sealed class CicloTotals
    {
        public int FilesProcessed { get; private set; }
        public int FilesSucceeded { get; private set; }
        public int FilesFailed { get; private set; }

        public void Add(FileTransferResult result)
        {
            FilesProcessed += result.FilesProcessed;
            FilesSucceeded += result.FilesSucceeded;
            FilesFailed += result.FilesFailed;
        }

        public void ForceFailure()
        {
            if (FilesFailed == 0) FilesFailed = 1;
        }
    }

    private async Task AtualizarParametrosAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IParametroRepository>();

            var parametros = await repository.BuscarParametrosExecucaoAsync(
                _aliasSistema, COD_HORA_INI, COD_HORA_FIM, COD_PERIODO, stoppingToken);

            if (parametros is null)
            {
                if (_ultimosParametros is not null)
                    _logger.LogWarning("Parâmetros do sistema indisponíveis. Mantendo último snapshot válido.");
                return;
            }

            _ultimosParametros = parametros;
            _interval = TimeSpan.FromMinutes(parametros.PeriodoMinutos);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao buscar parâmetros. Mantendo último snapshot válido.");
        }
    }

    private async Task<STA.Core.Data.Entities.ConexaoSftp?> BuscarConexaoSftpAsync(int cnConexaoSftp, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<STA.Core.Data.StaDbContext>();
            return await context.ConexoesSftp.FindAsync([cnConexaoSftp], ct);
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<STA.Core.Services.DestinoTransfer>> BuscarDestinosComRenameAsync(
        int? cnRota,
        IReadOnlyList<STA.Core.Models.TransferPath> nodes,
        CancellationToken stoppingToken)
    {
        if (cnRota is null)
        {
            // Fallback: sem rename, só pegar diretórios dos nós destino
            return nodes.Skip(1).Select(n => new STA.Core.Services.DestinoTransfer(n.DiretorioPrincipal, null)).ToList();
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<STA.Core.Data.StaDbContext>();
            var rotaDestinos = await context.RotaDestinos
                .Include(d => d.ConexaoSftp)
                .Where(d => d.CnRota == cnRota.Value && d.FlAtivo)
                .OrderBy(d => d.NrOrdem)
                .ToListAsync(stoppingToken);

            return rotaDestinos.Select(d => new STA.Core.Services.DestinoTransfer(
                d.DsDiretorioDestino,
                d.DsPadraoRename,
                d,
                d.ConexaoSftp
            )).ToList();
        }
        catch
        {
            return nodes.Skip(1).Select(n => new STA.Core.Services.DestinoTransfer(n.DiretorioPrincipal, null)).ToList();
        }
    }

    private async Task LimparLogsOrfaosAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<STA.Core.Data.StaDbContext>();
            var orfaos = await context.Logs
                .Where(l => l.IdStatusProcesso == "R")
                .ToListAsync(stoppingToken);

            if (orfaos.Count > 0)
            {
                foreach (var log in orfaos)
                {
                    log.IdStatusProcesso = "W";
                    log.DtFimProcesso = DateTime.UtcNow;
                }
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogWarning("Fechados {Count} log(s) de processo órfão(s) com status 'R' da sessão anterior.", orfaos.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao limpar logs órfãos na inicialização.");
        }
    }

    private async Task<bool> IsWorkerPausedAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var pauseService = scope.ServiceProvider.GetRequiredService<STA.Core.Services.IWorkerPauseService>();
        return await pauseService.IsPausedAsync(stoppingToken);
    }
}
