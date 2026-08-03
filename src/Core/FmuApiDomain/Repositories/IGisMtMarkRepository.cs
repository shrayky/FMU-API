using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.Repositories;

public interface IGisMtMarkRepository
{
    Task<GisMtMarkEntity?> Get(string id);

    Task<bool> Save(GisMtMarkEntity entity);

    Task<bool> SaveRange(IEnumerable<GisMtMarkEntity> entities);

    Task<Result<GisMtMarkEntity>> ChangeState(string sGtin, bool sold);

    Task<List<GisMtMarkEntity>> GetExpiredForCleanup(DateTime olderThanUtc, int limit);

    Task<bool> Delete(string id);

    /// <summary>
    /// Поиск марок остатка с пагинацией и опциональным отбором по товарной группе.
    /// </summary>
    Task<Result<GisMtMarkSearchResult>> Search(string searchTerm, int page, int pageSize, string? productGroup = null);
}
