using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STA.Api.Common;
using STA.Api.Dtos;
using STA.Core.Data;

namespace STA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/logs")]
public class LogsController : ControllerBase
{
    private readonly StaDbContext _context;

    public LogsController(StaDbContext context)
    {
        _context = context;
    }

    [HttpGet("processos")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LogProcessoDto>>>> GetProcessos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null,
        CancellationToken ct = default)
    {
        var query = _context.Logs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.IdStatusProcesso == status);
        if (de.HasValue)
            query = query.Where(l => l.DtInicio >= de.Value);
        if (ate.HasValue)
            query = query.Where(l => l.DtInicio <= ate.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.DtInicio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LogProcessoDto(
                l.CnLogProcesso,
                l.CnSistema,
                l.CnProcesso,
                l.DtInicio,
                l.DtFimProcesso,
                l.IdStatusProcesso,
                l.QtRegistrosProcessados,
                l.QtRegistrosErro,
                l.XmlObsProcesso))
            .ToListAsync(ct);

        return Ok(new ApiResponse<PaginatedResponse<LogProcessoDto>>(true,
            new PaginatedResponse<LogProcessoDto>(items, total, page, pageSize)));
    }

    [HttpGet("processos/{id:int}")]
    public async Task<ActionResult<ApiResponse<LogProcessoDto>>> GetProcessoById(int id, CancellationToken ct = default)
    {
        var log = await _context.Logs
            .AsNoTracking()
            .Where(l => l.CnLogProcesso == id)
            .Select(l => new LogProcessoDto(
                l.CnLogProcesso,
                l.CnSistema,
                l.CnProcesso,
                l.DtInicio,
                l.DtFimProcesso,
                l.IdStatusProcesso,
                l.QtRegistrosProcessados,
                l.QtRegistrosErro,
                l.XmlObsProcesso))
            .FirstOrDefaultAsync(ct);

        if (log is null)
            return NotFound(new ApiResponse<LogProcessoDto>(false, null, "Log de processo não encontrado."));

        return Ok(new ApiResponse<LogProcessoDto>(true, log));
    }

    [HttpGet("arquivos")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LogArquivoDto>>>> GetArquivos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? etapaId = null,
        [FromQuery] int? rotaId = null,
        [FromQuery] string? arquivo = null,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null,
        CancellationToken ct = default)
    {
        var query = _context.LogArquivos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.IdStatus == status);
        if (etapaId.HasValue)
            query = query.Where(l => l.CnEtapa == etapaId.Value);
        if (rotaId.HasValue)
            query = query.Where(l => l.CnRota == rotaId.Value);
        if (!string.IsNullOrEmpty(arquivo))
            query = query.Where(l => EF.Functions.ILike(l.NmArquivo, $"%{arquivo}%"));
        if (de.HasValue)
            query = query.Where(l => l.DtInicio >= de.Value);
        if (ate.HasValue)
            query = query.Where(l => l.DtInicio <= ate.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.DtInicio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LogArquivoDto(
                l.CnLogArquivo,
                l.CnLogProcesso,
                l.CnEtapa,
                l.CnRota,
                l.NmArquivo,
                l.DsDiretorioOrigem,
                l.DsDiretorioDestino,
                l.NrTamanhoBytes,
                l.DtInicio,
                l.DtFim,
                l.IdStatus,
                l.DsMensagem,
                l.FlCompactado,
                l.FlDescompactado))
            .ToListAsync(ct);

        return Ok(new ApiResponse<PaginatedResponse<LogArquivoDto>>(true,
            new PaginatedResponse<LogArquivoDto>(items, total, page, pageSize)));
    }

    [HttpGet("arquivos/{id:long}")]
    public async Task<ActionResult<ApiResponse<LogArquivoDto>>> GetArquivoById(long id, CancellationToken ct = default)
    {
        var log = await _context.LogArquivos
            .AsNoTracking()
            .Where(l => l.CnLogArquivo == id)
            .Select(l => new LogArquivoDto(
                l.CnLogArquivo,
                l.CnLogProcesso,
                l.CnEtapa,
                l.CnRota,
                l.NmArquivo,
                l.DsDiretorioOrigem,
                l.DsDiretorioDestino,
                l.NrTamanhoBytes,
                l.DtInicio,
                l.DtFim,
                l.IdStatus,
                l.DsMensagem,
                l.FlCompactado,
                l.FlDescompactado))
            .FirstOrDefaultAsync(ct);

        if (log is null)
            return NotFound(new ApiResponse<LogArquivoDto>(false, null, "Log de arquivo não encontrado."));

        return Ok(new ApiResponse<LogArquivoDto>(true, log));
    }
}
