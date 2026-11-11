namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Нужен ли в документе МОД (КПП или ФИАС): справочник True API требует его у LK_RECEIPT только
/// по причинам дистанционной продажи, продажи по образцам, вендинга, розницы и фасовки.
/// </summary>
public static class GisMtModRequirement
{
    private static readonly HashSet<string> LkReceiptReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "DISTANCE",
        "BY_SAMPLES",
        "VENDING",
        "RETAIL",
        GisMtWriteOffReasons.Packing
    };

    public static bool Required(string? kind, string? reason) =>
        kind == GisMtJournalKinds.ConnectTap
        || LkReceiptReasons.Contains(GisMtWriteOffReasons.Resolve(reason));
}
