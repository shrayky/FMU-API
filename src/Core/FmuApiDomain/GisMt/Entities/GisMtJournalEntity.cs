using FmuApiDomain.GisMt.Journal;
using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.GisMt.Entities;

/// <summary>
/// Акт журнала исходящих документов ГИС МТ: постановка на кран или списание молочной продукции.
/// </summary>
public class GisMtJournalEntity : IHaveStringId
{
    public string Id { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Reason { get; set; } = GisMtWriteOffReasons.Packing;

    /// <summary>
    /// Товарная группа ГИС МТ (параметр pg): молочная продукция, упакованная вода и т.д.
    /// </summary>
    public string ProductGroup { get; set; } = string.Empty;

    public string Status { get; set; } = GisMtJournalStatuses.Draft;

    public string Inn { get; set; } = string.Empty;

    public string Kpp { get; set; } = string.Empty;

    public string FiasId { get; set; } = string.Empty;

    public string Uid { get; set; } = string.Empty;

    public DateOnly DocDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public string CrptDocumentId { get; set; } = string.Empty;

    /// <summary>
    /// Тело документа, отправленное в ГИС МТ: хранится для разбора ошибок обработки.
    /// </summary>
    public string CrptBody { get; set; } = string.Empty;

    public string CrptStatus { get; set; } = string.Empty;

    public string CrptStatusText { get; set; } = string.Empty;

    /// <summary>
    /// Разбор отклонённых ГИС МТ кодов: статус и упаковка каждого.
    /// </summary>
    public string CrptErrorsDetail { get; set; } = string.Empty;

    public DateTime? CrptStatusCheckedAt { get; set; }

    public string ErrorText { get; set; } = string.Empty;

    public List<GisMtJournalLine> Lines { get; set; } = [];
}
