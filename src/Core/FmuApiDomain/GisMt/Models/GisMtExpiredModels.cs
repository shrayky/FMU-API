namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Просроченная марка остатка ГИС МТ для списания по сроку годности.
/// </summary>
public record GisMtExpiredMark(
    string Mark,
    string Cis,
    string Gtin,
    DateTime? ExpireDate);

/// <summary>
/// Строка списания по сроку годности: марка остатка и её КИ.
/// </summary>
public record GisMtExpiredLine(
    string Mark,
    string Cis);

/// <summary>
/// Подбор просроченных марок: сами марки и сколько упаковок отброшено.
/// </summary>
public record GisMtExpiredSelection(
    IReadOnlyList<GisMtExpiredMark> Marks,
    int ExcludedPackages);
