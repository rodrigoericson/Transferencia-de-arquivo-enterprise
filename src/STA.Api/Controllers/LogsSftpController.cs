using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STA.Api.Common;
using STA.Api.Dtos;
using STA.Core.Data;

namespace STA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/logs-sftp")]
[Produces("application/json")]
public class LogsSftpController : ControllerBase
{
    private readonly StaDbContext _context;

    public LogsSftpController(StaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<LogSftpDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? conexaoId = null,
        [FromQuery] string? tipo = null,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null,
        CancellationToken ct = default)
    {
        var query = _context.LogsSftp.AsNoTracking().AsQueryable();

        if (conexaoId.HasValue)
            query = query.Where(l => l.CnConexaoSftp == conexaoId.Value);

        if (!string.IsNullOrWhiteSpace(tipo))
            query = query.Where(l => l.IdTipo == tipo);

        if (de.HasValue)
        {
            var deUtc = DateTime.SpecifyKind(de.Value, DateTimeKind.Utc);
            query = query.Where(l => l.DtEvento >= deUtc);
        }

        if (ate.HasValue)
        {
            var ateUtc = DateTime.SpecifyKind(ate.Value, DateTimeKind.Utc).AddDays(1);
            query = query.Where(l => l.DtEvento < ateUtc);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(l => l.DtEvento)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LogSftpDto(
                l.CnLogSftp, l.CnConexaoSftp, l.CnRotaDestino,
                l.IdTipo, l.IdStatus, l.NmArquivo,
                l.NrTamanhoBytes, l.NrDuracaoMs, l.DsMensagem, l.DtEvento))
            .ToListAsync(ct);

        var result = new PaginatedResponse<LogSftpDto>(items, total, page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<LogSftpDto>>(true, result));
    }
}
