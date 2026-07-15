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
[Route("api/v1/destinos")]
public class DestinosController : ControllerBase
{
    private readonly StaDbContext _context;

    public DestinosController(StaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<DestinoDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? rotaId = null,
        [FromQuery] bool? ativo = null,
        CancellationToken ct = default)
    {
        var query = _context.RotaDestinos.AsNoTracking().AsQueryable();

        if (rotaId.HasValue)
            query = query.Where(d => d.CnRota == rotaId.Value);
        if (ativo.HasValue)
            query = query.Where(d => d.FlAtivo == ativo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(d => d.CnRota).ThenBy(d => d.NrOrdem)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DestinoDto(
                d.CnRotaDestino,
                d.CnRota,
                d.NrOrdem,
                d.DsDiretorioDestino,
                d.DsDescompactaDestino,
                d.FlAtivo))
            .ToListAsync(ct);

        var result = new PaginatedResponse<DestinoDto>(items, total, page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<DestinoDto>>(true, result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DestinoDto>>> GetById(int id, CancellationToken ct = default)
    {
        var destino = await _context.RotaDestinos
            .AsNoTracking()
            .Where(d => d.CnRotaDestino == id)
            .Select(d => new DestinoDto(
                d.CnRotaDestino,
                d.CnRota,
                d.NrOrdem,
                d.DsDiretorioDestino,
                d.DsDescompactaDestino,
                d.FlAtivo))
            .FirstOrDefaultAsync(ct);

        if (destino is null)
            return NotFound(new ApiResponse<DestinoDto>(false, null, "Destino não encontrado."));

        return Ok(new ApiResponse<DestinoDto>(true, destino));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DestinoDto>>> Create([FromBody] CreateDestinoDto dto, CancellationToken ct = default)
    {
        var rotaExists = await _context.Rotas.AnyAsync(r => r.CnRota == dto.CnRota, ct);
        if (!rotaExists)
            return BadRequest(new ApiResponse<DestinoDto>(false, null, "Rota não encontrada."));

        var destino = new RotaDestino
        {
            CnRota = dto.CnRota,
            NrOrdem = dto.NrOrdem,
            DsDiretorioDestino = dto.DsDiretorioDestino,
            DsDescompactaDestino = dto.DsDescompactaDestino,
            FlAtivo = true
        };

        _context.RotaDestinos.Add(destino);
        await _context.SaveChangesAsync(ct);

        TryCriarDiretorio(destino.DsDiretorioDestino);

        var result = new DestinoDto(
            destino.CnRotaDestino, destino.CnRota, destino.NrOrdem,
            destino.DsDiretorioDestino, destino.DsDescompactaDestino, destino.FlAtivo);

        return CreatedAtAction(nameof(GetById), new { id = destino.CnRotaDestino }, new ApiResponse<DestinoDto>(true, result));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<DestinoDto>>> Update(int id, [FromBody] UpdateDestinoDto dto, CancellationToken ct = default)
    {
        var destino = await _context.RotaDestinos.FindAsync([id], ct);
        if (destino is null)
            return NotFound(new ApiResponse<DestinoDto>(false, null, "Destino não encontrado."));

        destino.NrOrdem = dto.NrOrdem;
        destino.DsDiretorioDestino = dto.DsDiretorioDestino;
        destino.DsDescompactaDestino = dto.DsDescompactaDestino;
        destino.FlAtivo = dto.FlAtivo;

        await _context.SaveChangesAsync(ct);

        var result = new DestinoDto(
            destino.CnRotaDestino, destino.CnRota, destino.NrOrdem,
            destino.DsDiretorioDestino, destino.DsDescompactaDestino, destino.FlAtivo);

        return Ok(new ApiResponse<DestinoDto>(true, result));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct = default)
    {
        var destino = await _context.RotaDestinos.FindAsync([id], ct);
        if (destino is null)
            return NotFound(new ApiResponse<object>(false, null, "Destino não encontrado."));

        _context.RotaDestinos.Remove(destino);
        await _context.SaveChangesAsync(ct);

        return Ok(new ApiResponse<object>(true, null, "Destino removido."));
    }

    private static void TryCriarDiretorio(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try { Directory.CreateDirectory(path); } catch { }
    }
}
