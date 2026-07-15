using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using STA.Api.Common;
using STA.Api.Dtos;

namespace STA.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public ActionResult<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Validação simples — substituir por AD/LDAP em produção
        if (!ValidateCredentials(request.Username, request.Password, out var role))
            return Unauthorized(new ApiResponse<LoginResponse>(false, null, "Credenciais inválidas."));

        var token = GenerateToken(request.Username, role);
        var expiration = DateTime.UtcNow.AddHours(
            double.Parse(_config["Jwt:ExpirationHours"] ?? "8"));

        var response = new LoginResponse(token, expiration, request.Username, role);
        return Ok(new ApiResponse<LoginResponse>(true, response));
    }

    private bool ValidateCredentials(string username, string password, out string role)
    {
        // TODO: Substituir por validação AD/LDAP
        // Por enquanto, aceita credenciais configuráveis em appsettings
        role = "Admin";

        if (username == "admin" && password == "admin")
            return true;

        if (username == "viewer" && password == "viewer")
        {
            role = "Viewer";
            return true;
        }

        return false;
    }

    private string GenerateToken(string username, string role)
    {
        var secret = _config["Jwt:Secret"]!;
        var issuer = _config["Jwt:Issuer"] ?? "STA.Api";
        var audience = _config["Jwt:Audience"] ?? "STA.Client";
        var hours = double.Parse(_config["Jwt:ExpirationHours"] ?? "8");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
