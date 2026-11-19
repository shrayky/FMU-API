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

    /// <summary>
    /// Печатная группа, выбранная при создании: у организаций с одним ИНН разные КПП.
    /// </summary>
    public int PrintGroupId { get; set; }

    public string Kpp { get; set; } = string.Empty;

    public string FiasId { get; set; } = string.Empty;

    /// <summary>
    /// Название организации на момент создания: остаётся, если печатную группу удалили.
    /// </summary>
    public string OrganisationName { get; set; } = string.Empty;

    /// <summary>
    /// Номер документа на ТСД.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Терминал, с которого выгружен акт.
    /// </summary>
    public string TerminalId { get; set; } = string.Empty;

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
