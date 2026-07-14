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
    private readonly string _aliasSistema;

    private TimeSpan _interval = TimeSpan.FromMinutes(5);
    private ParametrosExecucao? _ultimosParametros;

    public Worker(
        ILogger<Worker> logger,
        IOptions<StaSettings> settings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _settings = settings;
        _scopeFactory = scopeFactory;
        _aliasSistema = settings.Value.NomeSistema;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("STA Worker iniciado em: {Time}", DateTimeOffset.Now);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
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
        var logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();

        var chains = await CarregarChainsAsync(etapaProvider, pathLoader, settings, logRepository, stoppingToken);
        if (chains is null)
            return;

        var dtInicio = DateTime.UtcNow;
        var totals = await ProcessarChainsAsync(chains, transferService, purgeService, settings, stoppingToken);

        await RegistrarResultadoAsync(logRepository, settings, dtInicio, totals, stoppingToken);
        ReportarResultado(totals);
    }

    private async Task<IReadOnlyList<STA.Core.Models.TransferChain>?> CarregarChainsAsync(
        IEtapaConfigProvider etapaProvider,
        IPathConfigLoader pathLoader,
        StaSettings settings,
        ILogRepository logRepository,
        CancellationToken stoppingToken)
    {
        // Tenta carregar do banco primeiro (ignora falha — pode não ter tabelas ainda)
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
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Tabelas de etapa não disponíveis no banco. Usando fallback XML.");
        }

        // Fallback: carregar do XML
        try
        {
            return pathLoader.CarregarCaminhos(settings.ArquivoPathsXml);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao carregar configuração de transferência.");
            await logRepository.InserirLogAsync(
                _aliasSistema, settings.CnProcesso, DateTime.UtcNow, "E", 0, 0, 0, 0,
                BuildLogObservacao("Leitura configuração", ex.Message), stoppingToken);
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
        StaSettings settings,
        CancellationToken stoppingToken)
    {
        var totals = new CicloTotals();

        foreach (var chain in chains)
        {
            for (int i = 0; i < chain.Nodes.Count - 1; i++)
            {
                var result = await transferService.TransferAsync(
                    chain.Nodes[i],
                    chain.Nodes[i].DiretorioPrincipal,
                    chain.Nodes[i + 1].DiretorioPrincipal,
                    settings.SobreEscreverArquivos,
                    settings.TimeoutCompactacaoMs,
                    stoppingToken);

                totals.Add(result);
            }

            if (chain.Nodes.Count > 0)
                purgeService.PurgeNode(chain.Nodes[^1]);
        }

        return totals;
    }

    private async Task RegistrarResultadoAsync(
        ILogRepository logRepository,
        StaSettings settings,
        DateTime dtInicio,
        CicloTotals totals,
        CancellationToken stoppingToken)
    {
        if (!settings.GeraLogSucessoBancoDados && totals.FilesFailed == 0)
            return;

        var status = totals.FilesFailed > 0 ? "W" : "O";
        var obs = BuildLogObservacao(
            "Transferencia de arquivos",
            $"Processados: {totals.FilesProcessed}, Sucesso: {totals.FilesSucceeded}, Falhas: {totals.FilesFailed}");

        await logRepository.InserirLogAsync(
            _aliasSistema, settings.CnProcesso, dtInicio, status,
            totals.FilesSucceeded, 0, totals.FilesFailed, 0, obs, stoppingToken);
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
}
