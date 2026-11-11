namespace FmuApiDomain.GisMt;

/// <summary>
/// Признаки источника марок остатка: документ ГИС МТ или ежедневная загрузка остатков.
/// </summary>
public static class GisMtMarkSources
{
    /// <summary>
    /// Марка пришла из загрузки остатков, а не из документа.
    /// </summary>
    public const string Stock = "stock";
}
