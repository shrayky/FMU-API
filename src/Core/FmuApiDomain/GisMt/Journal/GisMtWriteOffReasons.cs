namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Причины выбытия документа «Вывод из оборота» (LK_RECEIPT): фасовка и истечение срока годности.
/// </summary>
public static class GisMtWriteOffReasons
{
    public const string Packing = "PACKING";
    public const string Expiration = "EXPIRATION";

    /// <summary>
    /// Причина выбытия акта; пустое значение у старых актов считается фасовкой.
    /// </summary>
    public static string Resolve(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? Packing : reason.Trim().ToUpperInvariant();

    public static bool IsExpiration(string? reason) => Resolve(reason) == Expiration;
}
