namespace STA.Core.Services;

public static class SftpSchedulerHelper
{
    private static readonly Dictionary<string, DayOfWeek> DiasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["seg"] = DayOfWeek.Monday,
        ["ter"] = DayOfWeek.Tuesday,
        ["qua"] = DayOfWeek.Wednesday,
        ["qui"] = DayOfWeek.Thursday,
        ["sex"] = DayOfWeek.Friday,
        ["sab"] = DayOfWeek.Saturday,
        ["dom"] = DayOfWeek.Sunday,
    };

    public static bool IsDiaHabilitado(string dsDiasSemana, DateTime agora)
    {
        if (string.IsNullOrWhiteSpace(dsDiasSemana))
            return true;

        var dias = dsDiasSemana.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hoje = agora.DayOfWeek;

        foreach (var dia in dias)
        {
            if (DiasMap.TryGetValue(dia, out var mapped) && mapped == hoje)
                return true;
        }

        return false;
    }

    public static string? GetHorarioAtivo(string dsHorariosExecucao, DateTime agora, int toleranciaMinutos)
    {
        if (string.IsNullOrWhiteSpace(dsHorariosExecucao))
            return null;

        var horarios = dsHorariosExecucao.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var agoraTime = agora.TimeOfDay;

        foreach (var horario in horarios)
        {
            if (TimeSpan.TryParse(horario, out var scheduled))
            {
                var diff = agoraTime - scheduled;
                if (diff >= TimeSpan.Zero && diff <= TimeSpan.FromMinutes(toleranciaMinutos))
                    return horario;
            }
        }

        return null;
    }

    public static bool IsUltimoHorarioDoDia(string dsHorariosExecucao, string horarioAtual)
    {
        if (string.IsNullOrWhiteSpace(dsHorariosExecucao))
            return true;

        var horarios = dsHorariosExecucao
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .OrderBy(h => h)
            .ToList();

        return horarios.LastOrDefault() == horarioAtual;
    }
}
