namespace FmuApiDomain.Configuration.Options;

/// Проверка расписания установки обновлений.
public static class UpdateInstallSchedule
{
    /// Разрешена ли установка в указанное локальное время.
    /// Пустой список интервалов разрешает установку в любое время.
    public static bool IsWithinSchedule(TimeOnly now, IReadOnlyCollection<ScheduleTime>? intervals)
    {
        if (intervals is null || intervals.Count == 0)
            return true;

        foreach (var interval in intervals)
        {
            if (IsWithinInterval(now, interval))
                return true;
        }

        return false;
    }

    /// Начало больше окончания — интервал проходит через полночь.
    private static bool IsWithinInterval(TimeOnly now, ScheduleTime interval)
    {
        if (interval.BeginTime <= interval.EndTime)
            return now >= interval.BeginTime && now <= interval.EndTime;

        return now >= interval.BeginTime || now <= interval.EndTime;
    }
}
