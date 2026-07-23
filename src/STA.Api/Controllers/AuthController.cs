using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using STA.Api.Common;
using STA.Api.Dtos;
using STA.Core.Data.Repositories;
using STA.Core.Services;

namespace STA.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IAuthService _authService;
    private readonly IAuditService _audit;
    private readonly STA.Core.Data.StaDbContext _context;

    public AuthController(IConfiguration config, IAuthService authService, IAuditService audit, STA.Core.Data.StaDbContext context)
    {
        _config = config;
        _authService = authService;
        _audit = audit;
        _context = context;
    }

    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct = default)
    {
        var result = await _authService.AuthenticateAsync(request.Username, request.Password, ct);

        if (!result.Success)
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, result.ErrorMessage ?? "Credenciais inválidas."));

        var token = GenerateToken(result.Username!, result.Role!, result.CnUsuario);
        var expiration = DateTime.UtcNow.AddHours(
            double.Parse(_config["Jwt:ExpirationHours"] ?? "8"));

        await _audit.RegistrarAsync(result.CnUsuario, result.Username ?? "desconhecido", "AUTH", 0, "LOGIN", result.Username, ct);

        var response = new LoginResponse(token, expiration, result.Username!, result.Role!);
        return Ok(new ApiResponse<LoginResponse>(true, response));
    }

    [Authorize]
    [HttpPost("trocar-senha")]
    public async Task<ActionResult<ApiResponse<object>>> TrocarSenha([FromBody] TrocarSenhaRequest request, CancellationToken ct = default)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized(new ApiResponse<object>(false, null, "Usuário não identificado."));

        var usuario = await GetUsuarioAsync(username, ct);
        if (usuario is null)
            return NotFound(new ApiResponse<object>(false, null, "Usuário não encontrado."));

        if (!BCrypt.Net.BCrypt.Verify(request.SenhaAtual, usuario.DsSenhaHash))
            return BadRequest(new ApiResponse<object>(false, null, "Senha atual incorreta."));

        if (request.NovaSenha.Length < 8)
            return BadRequest(new ApiResponse<object>(false, null, "Nova senha deve ter no mínimo 8 caracteres."));

        usuario.DsSenhaHash = BCrypt.Net.BCrypt.HashPassword(request.NovaSenha, 12);
        await SaveAsync(ct);
        await _audit.RegistrarAsync("AUTH", 0, "PASSWORD", username, ct);

        return Ok(new ApiResponse<object>(true, null, "Senha alterada com sucesso."));
    }

    private string GenerateToken(string username, string role, int? cnUsuario)
    {
        var secret = _config["Jwt:Secret"]!;
        var issuer = _config["Jwt:Issuer"] ?? "STA.Api";
        var audience = _config["Jwt:Audience"] ?? "STA.Client";
        var hours = double.Parse(_config["Jwt:ExpirationHours"] ?? "8");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (cnUsuario.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, cnUsuario.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims.AsEnumerable(),
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<STA.Core.Data.Entities.Usuario?> GetUsuarioAsync(string username, CancellationToken ct)
    {
        return await _context.Usuarios.FirstOrDefaultAsync(
            u => u.NmUsuario == username, ct);
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}

public record TrocarSenhaRequest(string SenhaAtual, string NovaSenha);
