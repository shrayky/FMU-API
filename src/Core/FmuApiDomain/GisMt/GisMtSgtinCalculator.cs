namespace FmuApiDomain.GisMt;

/// <summary>
/// Вычисление sGTIN (GTIN + серийный номер) из кода маркировки.
/// </summary>
public static class GisMtSgtinCalculator
{
    private static readonly char Gs = (char)29;
    private const string GsEscape = @"\u001d";

    /// <summary>
    /// Формирует идентификатор марки: содержимое КИ без AI 01 и без криптохвоста.
    /// Пример: 0104603334003509215-DRAreEJwtMz → 04603334003509215
    /// </summary>
    public static string Calculate(string? cis, string? gtin)
    {
        var code = (cis ?? string.Empty).Trim();
        if (code.Length == 0)
            return NormalizeGtin(gtin);

        code = code.Replace(GsEscape, Gs.ToString());

        if (code.Length > 0 && code[0] == Gs)
            code = code[1..];

        // Потерян ведущий 0 у AI 01 (0104… → 104…)
        if (code.StartsWith('1') && code.Length >= 15 && !code.StartsWith("01", StringComparison.Ordinal))
        {
            var repaired = "0" + code;
            if (repaired.StartsWith("01", StringComparison.Ordinal))
                code = repaired;
        }

        // КИ с AI 01: убираем только "01", оставляем GTIN + хвост (включая 21 и серию)
        if (code.StartsWith("01", StringComparison.Ordinal) && code.Length >= 16)
            return CutCryptoTail(code[2..]);

        // Штучный табак: первые 21 символа
        if (code.Length == 29)
            return code[..21];

        // Уже sGTIN / printView
        if (!string.IsNullOrWhiteSpace(gtin))
        {
            var normalizedGtin = NormalizeGtin(gtin);
            if (code.StartsWith(normalizedGtin, StringComparison.Ordinal))
                return CutCryptoTail(code);
        }

        return CutCryptoTail(code);
    }

    /// <summary>
    /// Обрезает криптохвост после серийного номера.
    /// </summary>
    private static string CutCryptoTail(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        var gsPosition = body.IndexOf(Gs);
        if (gsPosition >= 0)
            return body[..gsPosition];

        // AI криптохвоста 91/92/93 — ищем только после GTIN
        foreach (var ai in new[] { "91", "92", "93" })
        {
            if (body.Length <= 15)
                break;

            var pos = body.IndexOf(ai, 15, StringComparison.Ordinal);
            if (pos > 0)
                return body[..pos];
        }

        // Дефис как разделитель криптохвоста (встречается во входных КИ)
        var dash = body.IndexOf('-');
        if (dash > 0)
            return body[..dash];

        return body;
    }

    private static string NormalizeGtin(string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
            return string.Empty;

        var value = gtin.Trim();
        if (value.Length == 13)
            return "0" + value;

        if (value.Length > 14)
            return value[^14..];

        return value.PadLeft(14, '0');
    }
}
