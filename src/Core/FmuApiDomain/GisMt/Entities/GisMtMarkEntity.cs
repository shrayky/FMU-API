using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.GisMt.Entities;

public class GisMtMarkEntity : IHaveStringId
{
    public string Id { get; set; } = string.Empty;

    public string SGtin { get; set; } = string.Empty;

    public string Cis { get; set; } = string.Empty;

    public string Gtin { get; set; } = string.Empty;

    public string OwnerInn { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string ProducerInn { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool Sold { get; set; }

    public DateTime? ExpireDate { get; set; }

    public string ProductGroup { get; set; } = string.Empty;

    public int ProductGroupId { get; set; }

    public bool IsTracking { get; set; }

    public string SourceDocumentId { get; set; } = string.Empty;

    public string OrganisationInn { get; set; } = string.Empty;

    public DateTime InfoLoadedAt { get; set; }
    public bool IsExpired => ExpireDate.HasValue && ExpireDate.Value < DateTime.UtcNow;
}
