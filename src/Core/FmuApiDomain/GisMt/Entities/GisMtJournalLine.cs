namespace FmuApiDomain.GisMt.Entities;

/// <summary>
/// Строка акта журнала: марка и её сведения для тела документа.
/// </summary>
public class GisMtJournalLine
{
    public string Mark { get; set; } = string.Empty;

    public string Cis { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Gtin { get; set; } = string.Empty;

    public DateOnly? ConnectDate { get; set; }

    public decimal? PricePerKg { get; set; }

    public decimal? QuantityKg { get; set; }

    public int? AmountKop { get; set; }
}
