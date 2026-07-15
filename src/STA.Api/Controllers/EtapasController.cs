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
[Route("api/v1/etapas")]
public class EtapasController : ControllerBase
{
    private readonly StaDbContext _context;

    public EtapasController(StaDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EtapaDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? ativo = null,
        CancellationToken ct = default)
    {
        var query = _context.Etapas.AsNoTracking().AsQueryable();

        if (ativo.HasValue)
            query = query.Where(e => e.FlAtivo == ativo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(e => e.NrOrdemExecucao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EtapaDto(
                e.CnEtapa,
                e.CnSistema,
                e.NmEtapa,
                e.FlAtivo,
                e.NrOrdemExecucao,
                e.HrInicioJanela,
                e.HrFimJanela,
                e.NrIntervaloMinutos,
                e.DtCriacao,
                e.DtAlteracao,
                e.Rotas.Count))
            .ToListAsync(ct);

        var result = new PaginatedResponse<EtapaDto>(items, total, page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<EtapaDto>>(true, result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<EtapaDto>>> GetById(int id, CancellationToken ct = default)
    {
        var etapa = await _context.Etapas
            .AsNoTracking()
            .Where(e => e.CnEtapa == id)
            .Select(e => new EtapaDto(
                e.CnEtapa,
                e.CnSistema,
                e.NmEtapa,
                e.FlAtivo,
                e.NrOrdemExecucao,
                e.HrInicioJanela,
                e.HrFimJanela,
                e.NrIntervaloMinutos,
                e.DtCriacao,
                e.DtAlteracao,
                e.Rotas.Count))
            .FirstOrDefaultAsync(ct);

        if (etapa is null)
            return NotFound(new ApiResponse<EtapaDto>(false, null, "Etapa não encontrada."));

        return Ok(new ApiResponse<EtapaDto>(true, etapa));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<EtapaDto>>> Create([FromBody] CreateEtapaDto dto, CancellationToken ct = default)
    {
        var sistema = await _context.Sistemas.FirstOrDefaultAsync(ct);
        if (sistema is null)
            return BadRequest(new ApiResponse<EtapaDto>(false, null, "Sistema não configurado no banco."));

        var etapa = new EtapaTransferencia
        {
            CnSistema = sistema.CnSistema,
            NmEtapa = dto.NmEtapa,
            NrOrdemExecucao = dto.NrOrdemExecucao,
            HrInicioJanela = dto.HrInicioJanela,
            HrFimJanela = dto.HrFimJanela,
            NrIntervaloMinutos = dto.NrIntervaloMinutos,
            FlAtivo = true,
            DtCriacao = DateTime.UtcNow
        };

        _context.Etapas.Add(etapa);
        await _context.SaveChangesAsync(ct);

        var result = new EtapaDto(
            etapa.CnEtapa, etapa.CnSistema, etapa.NmEtapa, etapa.FlAtivo,
            etapa.NrOrdemExecucao, etapa.HrInicioJanela, etapa.HrFimJanela,
            etapa.NrIntervaloMinutos, etapa.DtCriacao, etapa.DtAlteracao, 0);

        return CreatedAtAction(nameof(GetById), new { id = etapa.CnEtapa }, new ApiResponse<EtapaDto>(true, result));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<EtapaDto>>> Update(int id, [FromBody] UpdateEtapaDto dto, CancellationToken ct = default)
    {
        var etapa = await _context.Etapas.FindAsync([id], ct);
        if (etapa is null)
            return NotFound(new ApiResponse<EtapaDto>(false, null, "Etapa não encontrada."));

        etapa.NmEtapa = dto.NmEtapa;
        etapa.FlAtivo = dto.FlAtivo;
        etapa.NrOrdemExecucao = dto.NrOrdemExecucao;
        etapa.HrInicioJanela = dto.HrInicioJanela;
        etapa.HrFimJanela = dto.HrFimJanela;
        etapa.NrIntervaloMinutos = dto.NrIntervaloMinutos;
        etapa.DtAlteracao = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        var rotasCount = await _context.Rotas.CountAsync(r => r.CnEtapa == id, ct);
        var result = new EtapaDto(
            etapa.CnEtapa, etapa.CnSistema, etapa.NmEtapa, etapa.FlAtivo,
            etapa.NrOrdemExecucao, etapa.HrInicioJanela, etapa.HrFimJanela,
            etapa.NrIntervaloMinutos, etapa.DtCriacao, etapa.DtAlteracao, rotasCount);

        return Ok(new ApiResponse<EtapaDto>(true, result));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct = default)
    {
        var etapa = await _context.Etapas.FindAsync([id], ct);
        if (etapa is null)
            return NotFound(new ApiResponse<object>(false, null, "Etapa não encontrada."));

        _context.Etapas.Remove(etapa);
        await _context.SaveChangesAsync(ct);

        return Ok(new ApiResponse<object>(true, null, "Etapa removida."));
    }
}
