namespace STA.Core.Services.Transports;

public static class SftpPathValidator
{
    public static bool TryNormalize(string? path, out string normalizedPath, out string erro)
    {
        normalizedPath = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim();
        erro = string.Empty;

        if (normalizedPath.Length > 500)
        {
            erro = "Caminho remoto deve ter no máximo 500 caracteres.";
            return false;
        }

        if (normalizedPath.Contains('\\'))
        {
            erro = "Caminho remoto deve usar '/' como separador.";
            return false;
        }

        if (normalizedPath.Any(char.IsControl))
        {
            erro = "Caminho remoto contém caracteres inválidos.";
            return false;
        }

        var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(s => s == ".."))
        {
            erro = "Caminho remoto não pode conter segmento '..'.";
            return false;
        }

        return true;
    }
}
