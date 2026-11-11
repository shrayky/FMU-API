namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Ограничения журнала документов ГИС МТ.
/// </summary>
public static class GisMtJournalLimits
{
    /// <summary>
    /// Сколько просроченных марок подбирает кнопка «Заполнить» за одно нажатие.
    /// </summary>
    public const int MaxExpiredMarksPerFill = 500;

    /// <summary>
    /// Сколько товарных групп организации показывается в форме списания.
    /// </summary>
    public const int StockGroups = 100;
}
