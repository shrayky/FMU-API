using FmuApiDomain.GisMt.Entities;

namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Статусы обработки документа в ГИС МТ из справочника «Статусы документов» и их подписи для интерфейса.
/// </summary>
public static class GisMtCrptStatuses
{
    public const string InProgress = "IN_PROGRESS";
    public const string CheckedOk = "CHECKED_OK";
    public const string CheckedNotOk = "CHECKED_NOT_OK";
    public const string ProcessingError = "PROCESSING_ERROR";
    public const string ParseError = "PARSE_ERROR";
    public const string Accepted = "ACCEPTED";
    public const string WaitAcceptance = "WAIT_ACCEPTANCE";

    /// <summary>
    /// Подпись статуса для интерфейса; неизвестный код возвращается как есть.
    /// </summary>
    public static string Text(string? status) => (status ?? string.Empty).Trim().ToUpperInvariant() switch
    {
        InProgress => "Проверяется",
        CheckedOk => "Обработан",
        CheckedNotOk => "Обработан с ошибками",
        ProcessingError => "Техническая ошибка",
        ParseError => "Обработан с ошибками",
        Accepted => "Принят",
        WaitAcceptance => "Ожидает приёмку",
        "UNDEFINED" => "Статус не определён",
        "" => "Статус не получен",
        var other => other
    };

    /// <summary>
    /// Статусы, при которых ГИС МТ отказала в обработке документа: коды возвращаются в остаток.
    /// </summary>
    public static readonly string[] Failed = [CheckedNotOk, ParseError, ProcessingError];

    /// <summary>
    /// Статус окончательный: опрашивать документ дальше не нужно.
    /// </summary>
    public static bool IsFinal(string? status) => (status ?? string.Empty).Trim().ToUpperInvariant() is
        CheckedOk or CheckedNotOk or ParseError or ProcessingError or Accepted;

    /// <summary>
    /// ГИС МТ отказала в обработке документа.
    /// </summary>
    public static bool IsFailed(string? status) => (status ?? string.Empty).Trim().ToUpperInvariant() is
        CheckedNotOk or ParseError or ProcessingError;

    /// <summary>
    /// Акт ждёт статуса: отправлен, статус не окончательный, проверялся не позже указанного момента.
    /// </summary>
    public static bool NeedsPolling(GisMtJournalEntity document, DateTime checkedBefore, DateTime sentAfter)
    {
        if (document.Status != GisMtJournalStatuses.Sent)
            return false;

        if (string.IsNullOrWhiteSpace(document.CrptDocumentId))
            return false;

        if (IsFinal(document.CrptStatus))
            return false;

        if (document.SentAt.HasValue && document.SentAt.Value < sentAfter)
            return false;

        return document.CrptStatusCheckedAt is null || document.CrptStatusCheckedAt < checkedBefore;
    }
}
