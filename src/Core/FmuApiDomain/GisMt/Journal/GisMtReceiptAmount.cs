namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Сумма марки в копейках по базовой розничной цене за килограмм и количеству.
/// </summary>
public static class GisMtReceiptAmount
{
    public static int? Calculate(decimal? pricePerKg, decimal? quantityKg)
    {
        if (pricePerKg is null || pricePerKg.Value <= 0)
            return null;

        if (quantityKg is null || quantityKg.Value <= 0)
            return null;

        return (int)Math.Round(pricePerKg.Value * quantityKg.Value * 100m, MidpointRounding.AwayFromZero);
    }
}
