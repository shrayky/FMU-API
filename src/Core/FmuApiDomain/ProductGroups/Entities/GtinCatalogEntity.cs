using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.ProductGroups.Entities;

public class GtinCatalogEntity : IHaveStringId
{
    public string Id { get; set; } = string.Empty;

    public string Gtin { get; set; } = string.Empty;

    public int TrueApiGroupId { get; set; }
}
