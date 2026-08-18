using FmuApiDomain.ProductGroups.Entities;

namespace FmuApiDomain.ProductGroups.Models;

public class GtinCatalogSearchResult
{
    public List<GtinCatalogEntity> Items { get; set; } = [];
    public int Count { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
}
