using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STA.Api.Common;
using STA.Api.Dtos;
using STA.Core.Data.Repositories;

namespace STA.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/auditoria")]
[Produces("application/json")]
public class AuditoriaController : ControllerBase
{
    private readonly IAuditoriaRepository _repository;

    public AuditoriaController(IAuditoriaRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AuditoriaDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? usuario = null,
        [FromQuery] string? entidade = null,
        [FromQuery] string? acao = null,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _repository.ListarAsync(usuario, entidade, acao, de, ate, page, pageSize, ct);

        var dtos = items.Select(a => new AuditoriaDto(
            a.CnAuditoria,
            a.CnUsuario,
            a.NmUsuario,
            a.IdEntidade,
            a.IdReferencia,
            a.IdAcao,
            a.DtAcao,
            a.DsDetalhe
        )).ToList();

        var result = new PaginatedResponse<AuditoriaDto>(dtos, total, page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<AuditoriaDto>>(true, result));
    }
}
