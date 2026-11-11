namespace FmuApiDomain.GisMt;

/// <summary>
/// Сжатие списка КИ до уникальных: код, продолжающий уже известный КИ, считается тем же кодом с хвостом.
/// </summary>
public static class GisMtCisUniqueness
{
    /// <summary>
    /// Оставляет по одному КИ на код: короткий вариант побеждает, продолжения отбрасываются.
    /// </summary>
    public static List<string> Distinct(IEnumerable<string> cises)
    {
        var ordered = cises
            .Select(cis => (cis ?? string.Empty).Trim())
            .Where(cis => cis.Length > 0)
            .OrderBy(cis => cis.Length)
            .ToList();

        var result = new List<string>();

        foreach (var cis in ordered)
        {
            if (result.Any(kept => cis.StartsWith(kept, StringComparison.OrdinalIgnoreCase)))
                continue;

            result.Add(cis);
        }

        return result;
    }
}
