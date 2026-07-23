using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STA.Core.Data;
using STA.Core.Data.Entities;

namespace STA.Core.Services;

public record AuthResult(bool Success, string? Username, string? DisplayName, string? Role, string? ErrorMessage, int? CnUsuario = null);

public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly StaDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    private const int MaxTentativas = 5;
    private const int MinutosBloqueio = 15;

    public AuthService(StaDbContext context, IConfiguration config, ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        // 1. Tenta LDAP primeiro (se habilitado)
        var ldapEnabled = _config["Ldap:Enabled"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        if (ldapEnabled)
        {
            var ldapResult = TryLdapAuth(username, password);
            if (ldapResult.Success)
            {
                _logger.LogInformation("Login via LDAP bem-sucedido: '{User}'.", username);
                var cnUsuario = await UpsertUsuarioLdapAsync(username, ldapResult.DisplayName ?? username, ldapResult.Role ?? "Viewer", ct);
                return ldapResult with { CnUsuario = cnUsuario };
            }
        }

        // 2. Fallback: banco local
        return await TryLocalAuthAsync(username, password, ct);
    }

    private AuthResult TryLdapAuth(string username, string password)
    {
        var server = _config["Ldap:Server"];
        var baseDn = _config["Ldap:BaseDn"];
        var domain = _config["Ldap:Domain"];

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(baseDn))
            return new AuthResult(false, null, null, null, "LDAP não configurado.");

        try
        {
            var ldapId = new LdapDirectoryIdentifier(server, 636, false, false);
            var credential = new NetworkCredential($"{username}@{domain}", password);

            using var connection = new LdapConnection(ldapId);
            connection.SessionOptions.SecureSocketLayer = true;
            var skipCertValidation = _config["Ldap:SkipCertificateValidation"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            if (skipCertValidation)
                connection.SessionOptions.VerifyServerCertificate = (conn, cert) => true;
            connection.AuthType = AuthType.Basic;
            connection.Bind(credential);

            // Busca o usuário pra pegar grupos (username escaped RFC 4515)
            var escapedUsername = EscapeLdapFilter(username);
            var searchRequest = new SearchRequest(
                baseDn,
                $"(sAMAccountName={escapedUsername})",
                SearchScope.Subtree,
                "memberOf", "displayName");

            var response = (SearchResponse)connection.SendRequest(searchRequest);

            if (response.Entries.Count == 0)
                return new AuthResult(false, null, null, null, "Usuário não encontrado no AD.");

            var entry = response.Entries[0];
            var displayName = entry.Attributes["displayName"]?[0]?.ToString() ?? username;
            var memberOf = entry.Attributes["memberOf"];

            var role = ResolveRoleFromGroups(memberOf);

            return new AuthResult(true, username, displayName, role, null);
        }
        catch (LdapException ex)
        {
            _logger.LogDebug(ex, "Falha LDAP para '{User}': {Message}", username, ex.Message);
            return new AuthResult(false, null, null, null, "Credenciais inválidas.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro inesperado na autenticação LDAP para '{User}'.", username);
            return new AuthResult(false, null, null, null, "Credenciais inválidas.");
        }
    }

    private string ResolveRoleFromGroups(DirectoryAttribute? memberOf)
    {
        if (memberOf == null) return "Viewer";

        for (int i = 0; i < memberOf.Count; i++)
        {
            var group = memberOf[i]?.ToString() ?? "";
            if (group.Contains("STA-Admins", StringComparison.OrdinalIgnoreCase)) return "Admin";
            if (group.Contains("STA-Operators", StringComparison.OrdinalIgnoreCase)) return "Operator";
            if (group.Contains("STA-Viewers", StringComparison.OrdinalIgnoreCase)) return "Viewer";
        }

        return "Viewer";
    }

    private async Task<AuthResult> TryLocalAuthAsync(string username, string password, CancellationToken ct)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NmUsuario == username, ct);

        if (usuario is null)
            return new AuthResult(false, null, null, null, "Credenciais inválidas.");

        if (!usuario.FlAtivo)
            return new AuthResult(false, null, null, null, "Conta desativada.");

        // Verificar bloqueio
        if (usuario.DtBloqueadoAte.HasValue && usuario.DtBloqueadoAte.Value > DateTime.UtcNow)
            return new AuthResult(false, null, null, null, $"Conta bloqueada. Tente novamente em {MinutosBloqueio} minutos.");

        // Verificar senha
        if (!BCrypt.Net.BCrypt.Verify(password, usuario.DsSenhaHash))
        {
            usuario.NrTentativasFalhas++;
            if (usuario.NrTentativasFalhas >= MaxTentativas)
            {
                usuario.DtBloqueadoAte = DateTime.UtcNow.AddMinutes(MinutosBloqueio);
                _logger.LogWarning("Conta '{User}' bloqueada por {Min} minutos após {N} tentativas.", username, MinutosBloqueio, MaxTentativas);
            }
            await _context.SaveChangesAsync(ct);
            return new AuthResult(false, null, null, null, "Credenciais inválidas.");
        }

        // Login OK — resetar tentativas
        usuario.NrTentativasFalhas = 0;
        usuario.DtBloqueadoAte = null;
        usuario.DtUltimoLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Login local bem-sucedido: '{User}'.", username);
        return new AuthResult(true, usuario.NmUsuario, usuario.NmDisplay, usuario.IdRole, null, usuario.CnUsuario);
    }

    private async Task<int> UpsertUsuarioLdapAsync(string username, string displayName, string role, CancellationToken ct)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.NmUsuario == username, ct);

        if (usuario is not null)
        {
            usuario.NmDisplay = displayName;
            usuario.IdRole = role;
            usuario.DtUltimoLogin = DateTime.UtcNow;
            usuario.NrTentativasFalhas = 0;
        }
        else
        {
            usuario = new Usuario
            {
                NmUsuario = username,
                NmDisplay = displayName,
                DsSenhaHash = "!",
                IdRole = role,
                FlAtivo = true,
                DtCriacao = DateTime.UtcNow,
                DtUltimoLogin = DateTime.UtcNow
            };
            _context.Usuarios.Add(usuario);
        }

        await _context.SaveChangesAsync(ct);
        return usuario.CnUsuario;
    }

    private static string EscapeLdapFilter(string input)
    {
        return input
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}
