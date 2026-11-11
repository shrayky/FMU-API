namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// GTIN из КИ: AI 01 и 14 цифр в начале кода идентификации.
/// </summary>
public static class GisMtCisGtin
{
    public const int GtinLength = 14;

    private const string GtinAi = "01";

    public static string Extract(string? cis)
    {
        var value = (cis ?? string.Empty).Trim();

        if (!value.StartsWith(GtinAi, StringComparison.Ordinal))
            return string.Empty;

        if (value.Length < GtinAi.Length + GtinLength)
            return string.Empty;

        return value.Substring(GtinAi.Length, GtinLength);
    }
}
