using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STA.Api.Common;
using STA.Api.Dtos;
using STA.Core.Data;
using STA.Core.Data.Entities;
using STA.Core.Data.Repositories;
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
    private readonly ILogSftpRepository _logSftpRepository;
    private readonly IAuditService _audit;

    public ConexoesSftpController(
        StaDbContext context,
        ICredencialProtector protector,
        ISftpClientFactory sftpFactory,
        ILogSftpRepository logSftpRepository,
        IAuditService audit)
    {
        _context = context;
        _protector = protector;
        _sftpFactory = sftpFactory;
        _logSftpRepository = logSftpRepository;
        _audit = audit;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ConexaoSftpDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? ativo = null,
        CancellationToken ct = default)
    {
        (page, pageSize) = Common.PaginationHelper.Normalize(page, pageSize);
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
                c.DsCaminhoChavePrivada != null, c.DsHorariosExecucao, c.DsDiasSemana,
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
            c.DsCaminhoChavePrivada != null, c.DsHorariosExecucao, c.DsDiasSemana,
            c.FlArquivoObrigatorio, c.NrToleranciaMinutos, c.FlAtivo,
            c.DtCriacao, c.DtUltimoUso);

        return Ok(new ApiResponse<ConexaoSftpDto>(true, dto));
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConexaoSftpDto>>> Create([FromBody] CreateConexaoSftpDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DsSenhaPlaintext) && string.IsNullOrWhiteSpace(dto.DsCaminhoChavePrivada))
            return BadRequest(new ApiResponse<ConexaoSftpDto>(false, null, "Informe a senha ou o caminho da chave privada."));

        if (!string.IsNullOrWhiteSpace(dto.DsCaminhoChavePrivada) && !System.IO.File.Exists(dto.DsCaminhoChavePrivada))
            return BadRequest(new ApiResponse<ConexaoSftpDto>(false, null, "Arquivo de chave privada não encontrado ou inacessível."));

        if (!ValidarHorarios(dto.DsHorariosExecucao, out var erroHorario))
            return BadRequest(new ApiResponse<ConexaoSftpDto>(false, null, erroHorario));

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
            conexao.DsCaminhoChavePrivada != null, conexao.DsHorariosExecucao, conexao.DsDiasSemana,
            conexao.FlArquivoObrigatorio, conexao.NrToleranciaMinutos, conexao.FlAtivo,
            conexao.DtCriacao, conexao.DtUltimoUso);

        return CreatedAtAction(nameof(GetById), new { id = conexao.CnConexaoSftp }, new ApiResponse<ConexaoSftpDto>(true, result));
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ConexaoSftpDto>>> Update(int id, [FromBody] UpdateConexaoSftpDto dto, CancellationToken ct = default)
    {
        if (!ValidarHorarios(dto.DsHorariosExecucao, out var erroHorario))
            return BadRequest(new ApiResponse<ConexaoSftpDto>(false, null, erroHorario));

        var conexao = await _context.ConexoesSftp.FindAsync([id], ct);
        if (conexao is null)
            return NotFound(new ApiResponse<ConexaoSftpDto>(false, null, "Conexão SFTP não encontrada."));

        if (!string.IsNullOrWhiteSpace(dto.DsCaminhoChavePrivada) && !System.IO.File.Exists(dto.DsCaminhoChavePrivada))
            return BadRequest(new ApiResponse<ConexaoSftpDto>(false, null, "Arquivo de chave privada não encontrado ou inacessível."));

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
            conexao.DsCaminhoChavePrivada != null, conexao.DsHorariosExecucao, conexao.DsDiasSemana,
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
                new TestarConexaoResultDto(false, "Falha ao conectar. Verifique host, porta, credenciais e conectividade.")));
        }
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpPost("{id:int}/testar")]
    public async Task<ActionResult<ApiResponse<TestarConexaoResultDto>>> TestarConexaoExistente(int id, CancellationToken ct = default)
    {
        var conexao = await _context.ConexoesSftp.FindAsync([id], ct);
        if (conexao is null)
            return NotFound(new ApiResponse<TestarConexaoResultDto>(false, null, "Conexão não encontrada."));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var client = _sftpFactory.Criar(conexao, _protector);
            client.Connect();
            var connected = client.IsConnected;
            client.Disconnect();
            sw.Stop();

            var logRepo = _logSftpRepository;
            if (connected)
            {
                await logRepo.InserirAsync(new STA.Core.Data.Entities.LogSftp
                {
                    CnConexaoSftp = conexao.CnConexaoSftp,
                    IdTipo = "CONEXAO",
                    IdStatus = "S",
                    NrDuracaoMs = (int)sw.ElapsedMilliseconds,
                    DsMensagem = $"Teste manual OK em {sw.ElapsedMilliseconds}ms — {conexao.DsHost}:{conexao.NrPorta} (usuario: {conexao.DsUsuario})",
                    DtEvento = DateTime.UtcNow
                }, ct);

                return Ok(new ApiResponse<TestarConexaoResultDto>(true,
                    new TestarConexaoResultDto(true, $"Conectado em {sw.ElapsedMilliseconds}ms")));
            }

            return Ok(new ApiResponse<TestarConexaoResultDto>(true,
                new TestarConexaoResultDto(false, "Não foi possível estabelecer conexão.")));
        }
        catch (Exception ex)
        {
            sw.Stop();
            try
            {
                await _logSftpRepository.InserirAsync(new STA.Core.Data.Entities.LogSftp
                {
                    CnConexaoSftp = conexao.CnConexaoSftp,
                    IdTipo = "CONEXAO",
                    IdStatus = "E",
                    NrDuracaoMs = (int)sw.ElapsedMilliseconds,
                    DsMensagem = "Falha ao testar conexão SFTP",
                    DtEvento = DateTime.UtcNow
                }, ct);
            }
            catch { }

            return Ok(new ApiResponse<TestarConexaoResultDto>(true,
                new TestarConexaoResultDto(false, "Falha ao conectar. Verifique host, porta, credenciais e conectividade.")));
        }
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpGet("{id:int}/browse")]
    public async Task<ActionResult<ApiResponse<BrowseSftpResultDto>>> Browse(int id, [FromQuery] string? path = "/", CancellationToken ct = default)
    {
        var conexao = await _context.ConexoesSftp.FindAsync([id], ct);
        if (conexao is null)
            return NotFound(new ApiResponse<BrowseSftpResultDto>(false, null, "Conexão SFTP não encontrada."));

        if (!SftpPathValidator.TryNormalize(path, out var normalizedPath, out var erroPath))
            return BadRequest(new ApiResponse<BrowseSftpResultDto>(false, null, erroPath));

        try
        {
            using var client = _sftpFactory.Criar(conexao, _protector);
            client.Connect();

            if (!client.DirectoryExists(normalizedPath))
            {
                client.Disconnect();
                return Ok(new ApiResponse<BrowseSftpResultDto>(false, null, $"Diretório remoto não encontrado: {normalizedPath}"));
            }

            var entries = client.ListDirectoryDetailed(normalizedPath)
                .OrderByDescending(e => e.IsDirectory)
                .ThenBy(e => e.Name)
                .Select(e => new SftpRemoteEntryDto(
                    e.Name,
                    e.FullPath,
                    e.IsDirectory,
                    e.SizeBytes,
                    e.LastModifiedUtc))
                .ToList();

            client.Disconnect();

            var result = new BrowseSftpResultDto(normalizedPath, entries);
            return Ok(new ApiResponse<BrowseSftpResultDto>(true, result));
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return Ok(new ApiResponse<BrowseSftpResultDto>(false, null, "Erro ao listar diretório remoto. Verifique caminho, permissões e conectividade SFTP."));
        }
    }

    [Authorize(Roles = "Admin,Operator")]
    [HttpGet("{id:int}/validar-diretorio")]
    public async Task<ActionResult<ApiResponse<ValidarDiretorioSftpResultDto>>> ValidarDiretorio(int id, [FromQuery] string? path = "/", CancellationToken ct = default)
    {
        var conexao = await _context.ConexoesSftp.FindAsync([id], ct);
        if (conexao is null)
            return NotFound(new ApiResponse<ValidarDiretorioSftpResultDto>(false, null, "Conexão SFTP não encontrada."));

        if (!SftpPathValidator.TryNormalize(path, out var normalizedPath, out var erroPath))
            return BadRequest(new ApiResponse<ValidarDiretorioSftpResultDto>(false, null, erroPath));

        try
        {
            using var client = _sftpFactory.Criar(conexao, _protector);
            client.Connect();
            var exists = client.DirectoryExists(normalizedPath);
            client.Disconnect();

            var mensagem = exists
                ? "Diretório remoto encontrado."
                : $"Diretório remoto não encontrado: {normalizedPath}";

            return Ok(new ApiResponse<ValidarDiretorioSftpResultDto>(true,
                new ValidarDiretorioSftpResultDto(exists, mensagem)));
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return Ok(new ApiResponse<ValidarDiretorioSftpResultDto>(true,
                new ValidarDiretorioSftpResultDto(false, "Erro ao verificar diretório remoto. Verifique caminho, permissões e conectividade SFTP.")));
        }
    }

    private static bool ValidarHorarios(string dsHorarios, out string erro)
    {
        erro = string.Empty;
        if (string.IsNullOrWhiteSpace(dsHorarios))
        {
            erro = "Informe pelo menos um horário de execução.";
            return false;
        }

        var horarios = dsHorarios.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var unicos = new HashSet<string>();

        foreach (var h in horarios)
        {
            if (!TimeSpan.TryParse(h, out _))
            {
                erro = $"Horário inválido: '{h}'. Use formato HH:mm (ex: 08:00, 14:30).";
                return false;
            }
            if (!unicos.Add(h))
            {
                erro = $"Horário duplicado: '{h}'.";
                return false;
            }
        }

        return true;
    }
}
