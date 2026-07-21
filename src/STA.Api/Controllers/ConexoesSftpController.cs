using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STA.Api.Common;
using STA.Api.Dtos;
using STA.Core.Data;
using STA.Core.Data.Entities;
using STA.Core.Services;
using STA.Core.Services.Transports;

namespace STA.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/conexoes-sftp")]
[Produces("application/json")]
public class ConexoesSftpController : ControllerBase
{
    private readonly StaDbContext _context;
    private readonly ICredencialProtector _protector;
    private readonly ISftpClientFactory _sftpFactory;
    private readonly IAuditService _audit;

    public ConexoesSftpController(
        StaDbContext context,
        ICredencialProtector protector,
        ISftpClientFactory sftpFactory,
        IAuditService audit)
    {
        _context = context;
        _protector = protector;
        _sftpFactory = sftpFactory;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ConexaoSftpDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? ativo = null,
        CancellationToken ct = default)
    {
        var query = _context.ConexoesSftp.AsNoTracking().AsQueryable();

        if (ativo.HasValue)
            query = query.Where(c => c.FlAtivo == ativo.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.NmConexao)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConexaoSftpDto(
                c.CnConexaoSftp, c.NmConexao, c.DsHost, c.NrPorta, c.DsUsuario,
                c.DsSenhaCriptografada != null,
                c.DsCaminhoChavePrivada, c.DsHorariosExecucao, c.DsDiasSemana,
                c.FlArquivoObrigatorio, c.NrToleranciaMinutos, c.FlAtivo,
                c.DtCriacao, c.DtUltimoUso))
            .ToListAsync(ct);

        var result = new PaginatedResponse<ConexaoSftpDto>(items, total, page, pageSize);
        return Ok(new ApiResponse<PaginatedResponse<ConexaoSftpDto>>(true, result));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<ConexaoSftpDto>>> GetById(int id, CancellationToken ct = default)
    {
        var c = await _context.ConexoesSftp.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CnConexaoSftp == id, ct);

        if (c is null)
            return NotFound(new ApiResponse<ConexaoSftpDto>(false, null, "Conexão SFTP não encontrada."));

        var dto = new ConexaoSftpDto(
            c.CnConexaoSftp, c.NmConexao, c.DsHost, c.NrPorta, c.DsUsuario,
            c.DsSenhaCriptografada != null,
            c.DsCaminhoChavePrivada, c.DsHorariosExecucao, c.DsDiasSemana,
            c.FlArquivoObrigatorio, c.NrToleranciaMinutos, c.FlAtivo,
            c.DtCriacao, c.DtUltimoUso);

        return Ok(new ApiResponse<ConexaoSftpDto>(true, dto));
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConexaoSftpDto>>> Create([FromBody] CreateConexaoSftpDto dto, CancellationToken ct = default)
    {
        var conexao = new ConexaoSftp
        {
            NmConexao = dto.NmConexao,
            DsHost = dto.DsHost,
            NrPorta = dto.NrPorta,
            DsUsuario = dto.DsUsuario,
            DsSenhaCriptografada = !string.IsNullOrEmpty(dto.DsSenhaPlaintext)
                ? _protector.Proteger(dto.DsSenhaPlaintext)
                : null,
            DsCaminhoChavePrivada = dto.DsCaminhoChavePrivada,
            DsHorariosExecucao = dto.DsHorariosExecucao,
            DsDiasSemana = dto.DsDiasSemana,
            FlArquivoObrigatorio = dto.FlArquivoObrigatorio,
            NrToleranciaMinutos = dto.NrToleranciaMinutos,
            FlAtivo = true,
            DtCriacao = DateTime.UtcNow
        };

        _context.ConexoesSftp.Add(conexao);
        await _context.SaveChangesAsync(ct);
        await _audit.RegistrarAsync("CONEXAO_SFTP", conexao.CnConexaoSftp, "CREATE", conexao.NmConexao, ct);

        var result = new ConexaoSftpDto(
            conexao.CnConexaoSftp, conexao.NmConexao, conexao.DsHost, conexao.NrPorta, conexao.DsUsuario,
            conexao.DsSenhaCriptografada != null,
            conexao.DsCaminhoChavePrivada, conexao.DsHorariosExecucao, conexao.DsDiasSemana,
            conexao.FlArquivoObrigatorio, conexao.NrToleranciaMinutos, conexao.FlAtivo,
            conexao.DtCriacao, conexao.DtUltimoUso);

        return CreatedAtAction(nameof(GetById), new { id = conexao.CnConexaoSftp }, new ApiResponse<ConexaoSftpDto>(true, result));
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ConexaoSftpDto>>> Update(int id, [FromBody] UpdateConexaoSftpDto dto, CancellationToken ct = default)
    {
        var conexao = await _context.ConexoesSftp.FindAsync([id], ct);
        if (conexao is null)
            return NotFound(new ApiResponse<ConexaoSftpDto>(false, null, "Conexão SFTP não encontrada."));

        conexao.NmConexao = dto.NmConexao;
        conexao.DsHost = dto.DsHost;
        conexao.NrPorta = dto.NrPorta;
        conexao.DsUsuario = dto.DsUsuario;
        conexao.DsCaminhoChavePrivada = dto.DsCaminhoChavePrivada;
        conexao.DsHorariosExecucao = dto.DsHorariosExecucao;
        conexao.DsDiasSemana = dto.DsDiasSemana;
        conexao.FlArquivoObrigatorio = dto.FlArquivoObrigatorio;
        conexao.NrToleranciaMinutos = dto.NrToleranciaMinutos;
        conexao.FlAtivo = dto.FlAtivo;

        if (!string.IsNullOrEmpty(dto.DsSenhaPlaintext))
            conexao.DsSenhaCriptografada = _protector.Proteger(dto.DsSenhaPlaintext);

        await _context.SaveChangesAsync(ct);
        await _audit.RegistrarAsync("CONEXAO_SFTP", conexao.CnConexaoSftp, "UPDATE", conexao.NmConexao, ct);

        var result = new ConexaoSftpDto(
            conexao.CnConexaoSftp, conexao.NmConexao, conexao.DsHost, conexao.NrPorta, conexao.DsUsuario,
            conexao.DsSenhaCriptografada != null,
            conexao.DsCaminhoChavePrivada, conexao.DsHorariosExecucao, conexao.DsDiasSemana,
            conexao.FlArquivoObrigatorio, conexao.NrToleranciaMinutos, conexao.FlAtivo,
            conexao.DtCriacao, conexao.DtUltimoUso);

        return Ok(new ApiResponse<ConexaoSftpDto>(true, result));
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(int id, CancellationToken ct = default)
    {
        var conexao = await _context.ConexoesSftp.FindAsync([id], ct);
        if (conexao is null)
            return NotFound(new ApiResponse<object>(false, null, "Conexão SFTP não encontrada."));

        var emUso = await _context.RotaDestinos.AnyAsync(d => d.CnConexaoSftp == id, ct);
        if (emUso)
            return BadRequest(new ApiResponse<object>(false, null, "Conexão em uso por destinos. Remova os destinos antes."));

        var nmConexao = conexao.NmConexao;
        _context.ConexoesSftp.Remove(conexao);
        await _context.SaveChangesAsync(ct);
        await _audit.RegistrarAsync("CONEXAO_SFTP", id, "DELETE", nmConexao, ct);

        return Ok(new ApiResponse<object>(true, null, "Conexão SFTP removida."));
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost("testar")]
    public async Task<ActionResult<ApiResponse<TestarConexaoResultDto>>> TestarConexao([FromBody] TestarConexaoSftpDto dto, CancellationToken ct = default)
    {
        try
        {
            var conexaoTemp = new ConexaoSftp
            {
                NmConexao = "teste-temporario",
                DsHost = dto.DsHost,
                NrPorta = dto.NrPorta,
                DsUsuario = dto.DsUsuario,
                DsSenhaCriptografada = !string.IsNullOrEmpty(dto.DsSenhaPlaintext)
                    ? _protector.Proteger(dto.DsSenhaPlaintext)
                    : null,
                DsCaminhoChavePrivada = dto.DsCaminhoChavePrivada,
                DsHorariosExecucao = "00:00",
                FlAtivo = true
            };

            using var client = _sftpFactory.Criar(conexaoTemp, _protector);
            client.Connect();
            var connected = client.IsConnected;
            client.Disconnect();

            if (connected)
                return Ok(new ApiResponse<TestarConexaoResultDto>(true,
                    new TestarConexaoResultDto(true, $"Conectado com sucesso em {dto.DsHost}:{dto.NrPorta}")));

            return Ok(new ApiResponse<TestarConexaoResultDto>(true,
                new TestarConexaoResultDto(false, "Não foi possível estabelecer conexão.")));
        }
        catch (Exception ex)
        {
            return Ok(new ApiResponse<TestarConexaoResultDto>(true,
                new TestarConexaoResultDto(false, $"Falha: {ex.Message}")));
        }
    }
}
