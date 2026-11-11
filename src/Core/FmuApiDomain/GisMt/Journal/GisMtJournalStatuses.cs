namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Статусы акта журнала документов ГИС МТ.
/// </summary>
public static class GisMtJournalStatuses
{
    public const string Draft = "draft";

    /// <summary>
    /// Отправка идёт: акт заблокирован от правки, удаления и повторной отправки.
    /// </summary>
    public const string Sending = "sending";

    public const string Sent = "sent";

    public const string Error = "error";

    /// <summary>
    /// Акт заблокирован на время отправки.
    /// </summary>
    public static bool IsLocked(string? status) =>
        string.Equals((status ?? string.Empty).Trim(), Sending, StringComparison.OrdinalIgnoreCase);
}
