using System.Collections.Concurrent;

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

    private static readonly ConcurrentDictionary<string, TimeSpan[]> _scheduleCache = new();

    private static TimeSpan[] GetParsedHorarios(string dsHorariosExecucao)
    {
        return _scheduleCache.GetOrAdd(dsHorariosExecucao, key =>
        {
            if (!TimeSpanHelper.TryParseHorarios(key, out var horarios, out _))
                return Array.Empty<TimeSpan>();
            return horarios.ToArray();
        });
    }

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

        var horarios = GetParsedHorarios(dsHorariosExecucao);
        if (horarios.Length == 0)
            return null;

        var agoraTime = agora.TimeOfDay;

        foreach (var scheduled in horarios)
        {
            var diff = agoraTime - scheduled;
            if (diff >= TimeSpan.Zero && diff <= TimeSpan.FromMinutes(toleranciaMinutos))
                return scheduled.ToString(@"hh\:mm");
        }

        return null;
    }

    public static bool IsUltimoHorarioDoDia(string dsHorariosExecucao, string horarioAtual)
    {
        if (string.IsNullOrWhiteSpace(dsHorariosExecucao))
            return true;

        if (!TimeSpanHelper.TryParseHorarioExato(horarioAtual, out var atual))
            return false;

        var cached = GetParsedHorarios(dsHorariosExecucao);
        if (cached.Length == 0)
            return true;

        return cached[^1] == atual;
    }
}
