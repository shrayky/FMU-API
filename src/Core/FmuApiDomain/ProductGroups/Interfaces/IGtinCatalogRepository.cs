using FmuApiDomain.ProductGroups.Entities;
using FmuApiDomain.ProductGroups.Models;

namespace FmuApiDomain.ProductGroups.Interfaces;

public interface IGtinCatalogRepository
{
    Task<GtinCatalogEntity?> Get(string gtin);

    Task<bool> Save(GtinCatalogEntity entity);

    Task<GtinCatalogSearchResult> Search(string searchTerm, int page, int pageSize);
}
