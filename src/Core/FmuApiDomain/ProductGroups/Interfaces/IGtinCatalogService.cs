using FmuApiDomain.ProductGroups.Entities;
using FmuApiDomain.ProductGroups.Models;

namespace FmuApiDomain.ProductGroups.Interfaces;

public interface IGtinCatalogService
{
    Task<GtinCatalogEntity?> Get(string gtin);

    Task SaveFromOnlineCheck(string gtin, int trueApiGroupId);

    Task<GtinCatalogSearchResult> Search(string searchTerm, int page, int pageSize);
}
