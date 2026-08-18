namespace FmuApiDomain.ProductGroups;

public static class GtinCalculator
{
    /// <summary>
    /// Извлекает GTIN (14 цифр) из sGTIN.
    /// </summary>
    public static string FromSgtin(string sgtin)
    {
        if (string.IsNullOrWhiteSpace(sgtin))
            return string.Empty;

        var value = sgtin.Trim();

        if (value.Length < 14)
            return value;

        return value[..14];
    }
}
