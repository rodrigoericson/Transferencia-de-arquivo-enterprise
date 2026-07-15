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
[Route("api/v1/rotas")]
public class RotasController : ControllerBase
{
    private readonly StaDbContext _context;

    public RotasController(StaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<RotaDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? etapaId = null,
        [FromQuery] bool? ativo = null,
        CancellationToken ct = default)
    {
        var query = _context.Rotas.AsNoTracking().AsQueryable();

        if (etapaId.HasValue)
            query = query.Where(r => r.CnEtapa == etapaId.Value);
        if (ativo.HasValue)
            query = query.Where(r => r.FlAtivo == ativo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(r => r.CnEtapa).ThenBy(r => r.NrOrdem)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RotaDto(
                r.CnRota,
                r.CnEtapa,
                r.NrOrdem,
                r.DsDiretorioOrigem,
                r.DsDiretorioBackup,
                r.DsMascaraArquivo,
                r.DsCompactaOrigemTipo,
                r.NrDiasExcluir,
                r.NrTamanhoInicialBytes,
                r.NrTamanhoFinalBytes,
                r.FlExcluirOrigem,
                r.FlAtivo,
                r.Destinos.Count))
            .ToListAsync(ct);

        var result = new PaginatedResponse<RotaDto>(items, total, page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<RotaDto>>(true, result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<RotaDto>>> GetById(int id, CancellationToken ct = default)
    {
        var rota = await _context.Rotas
            .AsNoTracking()
            .Where(r => r.CnRota == id)
            .Select(r => new RotaDto(
                r.CnRota,
                r.CnEtapa,
                r.NrOrdem,
                r.DsDiretorioOrigem,
                r.DsDiretorioBackup,
                r.DsMascaraArquivo,
                r.DsCompactaOrigemTipo,
                r.NrDiasExcluir,
                r.NrTamanhoInicialBytes,
                r.NrTamanhoFinalBytes,
                r.FlExcluirOrigem,
                r.FlAtivo,
                r.Destinos.Count))
            .FirstOrDefaultAsync(ct);

        if (rota is null)
            return NotFound(new ApiResponse<RotaDto>(false, null, "Rota não encontrada."));

        return Ok(new ApiResponse<RotaDto>(true, rota));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RotaDto>>> Create([FromBody] CreateRotaDto dto, CancellationToken ct = default)
    {
        var etapaExists = await _context.Etapas.AnyAsync(e => e.CnEtapa == dto.CnEtapa, ct);
        if (!etapaExists)
            return BadRequest(new ApiResponse<RotaDto>(false, null, "Etapa não encontrada."));

        var rota = new RotaTransferencia
        {
            CnEtapa = dto.CnEtapa,
            NrOrdem = dto.NrOrdem,
            DsDiretorioOrigem = dto.DsDiretorioOrigem,
            DsDiretorioBackup = dto.DsDiretorioBackup,
            DsMascaraArquivo = dto.DsMascaraArquivo,
            DsCompactaOrigemTipo = dto.DsCompactaOrigemTipo,
            NrDiasExcluir = dto.NrDiasExcluir,
            NrTamanhoInicialBytes = dto.NrTamanhoInicialBytes,
            NrTamanhoFinalBytes = dto.NrTamanhoFinalBytes,
            FlExcluirOrigem = dto.FlExcluirOrigem,
            FlAtivo = true
        };

        _context.Rotas.Add(rota);
        await _context.SaveChangesAsync(ct);

        var result = new RotaDto(
            rota.CnRota, rota.CnEtapa, rota.NrOrdem, rota.DsDiretorioOrigem,
            rota.DsDiretorioBackup, rota.DsMascaraArquivo, rota.DsCompactaOrigemTipo,
            rota.NrDiasExcluir, rota.NrTamanhoInicialBytes, rota.NrTamanhoFinalBytes,
            rota.FlExcluirOrigem, rota.FlAtivo, 0);

        return CreatedAtAction(nameof(GetById), new { id = rota.CnRota }, new ApiResponse<RotaDto>(true, result));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<RotaDto>>> Update(int id, [FromBody] UpdateRotaDto dto, CancellationToken ct = default)
    {
        var rota = await _context.Rotas.FindAsync([id], ct);
        if (rota is null)
            return NotFound(new ApiResponse<RotaDto>(false, null, "Rota não encontrada."));

        rota.NrOrdem = dto.NrOrdem;
        rota.DsDiretorioOrigem = dto.DsDiretorioOrigem;
        rota.DsDiretorioBackup = dto.DsDiretorioBackup;
        rota.DsMascaraArquivo = dto.DsMascaraArquivo;
        rota.DsCompactaOrigemTipo = dto.DsCompactaOrigemTipo;
        rota.NrDiasExcluir = dto.NrDiasExcluir;
        rota.NrTamanhoInicialBytes = dto.NrTamanhoInicialBytes;
        rota.NrTamanhoFinalBytes = dto.NrTamanhoFinalBytes;
        rota.FlExcluirOrigem = dto.FlExcluirOrigem;
        rota.FlAtivo = dto.FlAtivo;

        await _context.SaveChangesAsync(ct);

        var destinosCount = await _context.RotaDestinos.CountAsync(d => d.CnRota == id, ct);
        var result = new RotaDto(
            rota.CnRota, rota.CnEtapa, rota.NrOrdem, rota.DsDiretorioOrigem,
            rota.DsDiretorioBackup, rota.DsMascaraArquivo, rota.DsCompactaOrigemTipo,
            rota.NrDiasExcluir, rota.NrTamanhoInicialBytes, rota.NrTamanhoFinalBytes,
            rota.FlExcluirOrigem, rota.FlAtivo, destinosCount);

        return Ok(new ApiResponse<RotaDto>(true, result));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct = default)
    {
        var rota = await _context.Rotas.FindAsync([id], ct);
        if (rota is null)
            return NotFound(new ApiResponse<object>(false, null, "Rota não encontrada."));

        _context.Rotas.Remove(rota);
        await _context.SaveChangesAsync(ct);

        return Ok(new ApiResponse<object>(true, null, "Rota removida."));
    }
}
