using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STA.Api.Common;
using STA.Api.Dtos;
using STA.Core.Data;
using STA.Core.Data.Entities;

namespace STA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/worker")]
public class WorkerController : ControllerBase
{
    private const int COD_WORKER_PAUSED = 4;

    private readonly StaDbContext _context;

    public WorkerController(StaDbContext context)
    {
        _context = context;
    }

    [HttpGet("status")]
    public async Task<ActionResult<ApiResponse<WorkerStatusDto>>> GetStatus(CancellationToken ct = default)
    {
        var isPaused = await IsPausedAsync(ct);

        var ultimoCiclo = await _context.Logs
            .AsNoTracking()
            .OrderByDescending(l => l.DtInicio)
            .FirstOrDefaultAsync(ct);

        var hoje = DateTime.UtcNow.Date;
        var arquivosHoje = await _context.LogArquivos
            .AsNoTracking()
            .CountAsync(l => l.DtInicio >= hoje, ct);

        var errosHoje = await _context.LogArquivos
            .AsNoTracking()
            .CountAsync(l => l.DtInicio >= hoje && l.IdStatus == "E", ct);

        var result = new WorkerStatusDto(
            Status: isPaused ? "pausado" : "rodando",
            UltimoCiclo: ultimoCiclo?.DtInicio,
            UltimoCicloStatus: ultimoCiclo?.IdStatusProcesso,
            ArquivosHoje: arquivosHoje,
            ErrosHoje: errosHoje);

        return Ok(new ApiResponse<WorkerStatusDto>(true, result));
    }

    [HttpGet("execucao")]
    public async Task<ActionResult<ApiResponse<ExecucaoDto>>> GetExecucao(CancellationToken ct = default)
    {
        var isPaused = await IsPausedAsync(ct);

        // Verificar se há ciclo aberto (status 'R') — indica execução em andamento
        var cicloAberto = await _context.Logs
            .AsNoTracking()
            .Where(l => l.IdStatusProcesso == "R")
            .OrderByDescending(l => l.DtInicio)
            .FirstOrDefaultAsync(ct);

        // Último ciclo concluído
        var ultimoCiclo = await _context.Logs
            .AsNoTracking()
            .Where(l => l.IdStatusProcesso != "R")
            .OrderByDescending(l => l.DtInicio)
            .FirstOrDefaultAsync(ct);

        // Última etapa processada (último log de arquivo do ciclo atual)
        string? etapaAtual = null;
        if (cicloAberto is not null)
        {
            var ultimoArquivo = await _context.LogArquivos
                .AsNoTracking()
                .Where(la => la.CnLogProcesso == cicloAberto.CnLogProcesso)
                .OrderByDescending(la => la.DtInicio)
                .FirstOrDefaultAsync(ct);

            if (ultimoArquivo?.CnEtapa is not null)
            {
                var etapa = await _context.Etapas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.CnEtapa == ultimoArquivo.CnEtapa, ct);
                etapaAtual = etapa?.NmEtapa;
            }
        }

        var executando = cicloAberto is not null;

        // Calcula duração do último ciclo
        TimeSpan? duracaoUltimoCiclo = null;
        if (ultimoCiclo?.DtFimProcesso != null)
            duracaoUltimoCiclo = ultimoCiclo.DtFimProcesso.Value - ultimoCiclo.DtInicio;

        // Próximo ciclo: último fim + intervalo (5 min)
        // Se já passou, não recalcula (frontend mostra "Aguardando")
        DateTime? proximoCiclo = null;
        if (!isPaused && !executando && ultimoCiclo?.DtFimProcesso != null)
        {
            proximoCiclo = ultimoCiclo.DtFimProcesso.Value.AddMinutes(5);
        }

        var result = new ExecucaoDto(
            Executando: executando,
            Pausado: isPaused,
            EtapaAtual: etapaAtual,
            CicloIniciadoEm: cicloAberto?.DtInicio,
            UltimoCicloFim: ultimoCiclo?.DtFimProcesso,
            ProximoCicloEm: proximoCiclo,
            DuracaoUltimoCicloMs: (long?)(duracaoUltimoCiclo?.TotalMilliseconds)
        );

        return Ok(new ApiResponse<ExecucaoDto>(true, result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("pause")]
    public async Task<ActionResult<ApiResponse<object>>> Pause(CancellationToken ct = default)
    {
        await SetPausedAsync(true, ct);
        return Ok(new ApiResponse<object>(true, null, "Worker pausado. O ciclo atual será concluído antes da pausa."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("resume")]
    public async Task<ActionResult<ApiResponse<object>>> Resume(CancellationToken ct = default)
    {
        await SetPausedAsync(false, ct);
        return Ok(new ApiResponse<object>(true, null, "Worker retomado. Próximo ciclo será executado normalmente."));
    }

    private async Task<bool> IsPausedAsync(CancellationToken ct)
    {
        var param = await _context.Parametros
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CnParametroSistema == COD_WORKER_PAUSED, ct);

        return param?.CdParametroSistema == "true";
    }

    private async Task SetPausedAsync(bool paused, CancellationToken ct)
    {
        var param = await _context.Parametros
            .FirstOrDefaultAsync(p => p.CnParametroSistema == COD_WORKER_PAUSED, ct);

        if (param is not null)
        {
            param.CdParametroSistema = paused ? "true" : "false";
        }
        else
        {
            var sistema = await _context.Sistemas.FirstAsync(ct);
            _context.Parametros.Add(new ParametroSistema
            {
                CnParametroSistema = COD_WORKER_PAUSED,
                CnSistema = sistema.CnSistema,
                CdParametroSistema = paused ? "true" : "false"
            });
        }

        await _context.SaveChangesAsync(ct);
    }
}
