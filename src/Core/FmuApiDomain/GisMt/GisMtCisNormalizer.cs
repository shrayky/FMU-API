namespace FmuApiDomain.GisMt;

/// <summary>
/// Нормализация КИ для True API: 01 + GTIN + 21 + SN (до GS).
/// </summary>
public static class GisMtCisNormalizer
{
    private static readonly char Gs = (char)29;
    private const string GsEscape = @"\u001d";

    /// <summary>
    /// Возвращает КИ без криптохвоста: 01 + GTIN + 21 + серийный номер.
    /// </summary>
    public static string ToCis(string? markCode)
    {
        var code = (markCode ?? string.Empty).Trim();
        if (code.Length == 0)
            return string.Empty;

        code = code.Replace(GsEscape, Gs.ToString());

        if (code[0] == Gs)
            code = code[1..];

        var gsPosition = code.IndexOf(Gs);
        if (gsPosition >= 0)
            return code[..gsPosition];

        if (code.Length == 29)
            return code[..21];

        return code;
    }
}
