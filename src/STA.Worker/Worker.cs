using Microsoft.Extensions.Options;
using STA.Worker.Data.Repositories;
using STA.Worker.Services;
using STA.Worker.Settings;

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
        var pathLoader = scope.ServiceProvider.GetRequiredService<IPathConfigLoader>();
        var transferService = scope.ServiceProvider.GetRequiredService<IFileTransferService>();
        var logRepository = scope.ServiceProvider.GetRequiredService<ILogRepository>();

        IReadOnlyList<Models.TransferChain> chains;
        try
        {
            chains = pathLoader.CarregarCaminhos(settings.ArquivoPathsXml);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falha ao carregar paths.xml de '{Path}'.", settings.ArquivoPathsXml);
            await logRepository.InserirLogAsync(
                _aliasSistema, settings.CnProcesso, DateTime.UtcNow, "E", 0, 0, 0, 0,
                $"<Etapa>Leitura XML</Etapa><Observacao>{ex.Message}</Observacao>", stoppingToken);
            return;
        }

        var dtInicio = DateTime.UtcNow;
        int totalProcessed = 0, totalSucceeded = 0, totalFailed = 0;

        foreach (var chain in chains)
        {
            for (int i = 0; i < chain.Nodes.Count - 1; i++)
            {
                var sourceNode = chain.Nodes[i];
                var destNode = chain.Nodes[i + 1];

                var result = await transferService.TransferAsync(
                    sourceNode,
                    sourceNode.DiretorioPrincipal,
                    destNode.DiretorioPrincipal,
                    settings.SobreEscreverArquivos,
                    settings.TimeoutCompactacaoMs,
                    stoppingToken);

                totalProcessed += result.FilesProcessed;
                totalSucceeded += result.FilesSucceeded;
                totalFailed += result.FilesFailed;
            }
        }

        var status = totalFailed > 0 ? "W" : "O";
        var obs = $"<Etapa>Transferencia de arquivos</Etapa><Observacao>Processados: {totalProcessed}, Sucesso: {totalSucceeded}, Falhas: {totalFailed}</Observacao>";

        if (settings.GeraLogSucessoBancoDados || totalFailed > 0)
        {
            await logRepository.InserirLogAsync(
                _aliasSistema, settings.CnProcesso, dtInicio, status,
                totalSucceeded, 0, totalFailed, 0, obs, stoppingToken);
        }

        if (totalFailed > 0)
            _logger.LogWarning("Ciclo com falhas: {Processed} processados, {Succeeded} OK, {Failed} erros.", totalProcessed, totalSucceeded, totalFailed);
        else if (totalProcessed > 0)
            _logger.LogInformation("Ciclo concluído: {Processed} arquivos transferidos com sucesso.", totalSucceeded);
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
