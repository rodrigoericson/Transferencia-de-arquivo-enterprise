using System.Globalization;
using System.Text.RegularExpressions;

namespace STA.Core.Services;

public static class TimeSpanHelper
{
    private static readonly Regex HhMmRegex = new(@"^([0-1][0-9]|2[0-3]):([0-5][0-9])$", RegexOptions.Compiled);

    /// <summary>
    /// Valida e parseia um horário no formato exato HH:mm (ex: "08:00", "23:59").
    /// Rejeita formatos alternativos como "8:00", "08:00:00", ou "1.08:00".
    /// </summary>
    public static bool TryParseHorarioExato(string? input, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        if (!HhMmRegex.IsMatch(trimmed))
            return false;

        return TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Valida uma lista de horários separados por vírgula, com deduplicação por valor (não string).
    /// Retorna lista dedupada de TimeSpan ordenada, ou erro se qualquer horário for inválido.
    /// </summary>
    public static bool TryParseHorarios(string? input, out List<TimeSpan> horarios, out string erro)
    {
        horarios = [];
        erro = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            erro = "Informe pelo menos um horário de execução.";
            return false;
        }

        var partes = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var vistos = new HashSet<TimeSpan>();

        foreach (var parte in partes)
        {
            if (!TryParseHorarioExato(parte, out var ts))
            {
                erro = $"Horário inválido: '{parte}'. Use formato exato HH:mm (ex: 08:00, 14:30).";
                return false;
            }

            if (!vistos.Add(ts))
            {
                erro = $"Horário duplicado: '{parte}' (equivalente a {ts:hh\\:mm}).";
                return false;
            }

            horarios.Add(ts);
        }

        horarios.Sort();
        return true;
    }
}
