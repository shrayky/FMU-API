using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

public interface IGisMtMarkRepository
{
    Task<GisMtMarkEntity?> Get(string id);

    Task<List<GisMtMarkEntity>> GetListById(List<string> ids);

    Task<bool> Save(GisMtMarkEntity entity);

    Task<bool> SaveRange(IEnumerable<GisMtMarkEntity> entities);

    Task<Result<GisMtMarkEntity>> ChangeState(string sGtin, bool sold);

    Task<List<GisMtMarkEntity>> GetExpiredForCleanup(DateTime olderThanUtc, int limit);

    /// <summary>
    /// Непроданные марки организации по товарной группе, сведения о которых загружены раньше указанного момента.
    /// </summary>
    Task<List<GisMtMarkEntity>> GetUnsoldLoadedBefore(
        string organisationInn,
        string productGroup,
        DateTime loadedBeforeUtc,
        int limit);

    /// <summary>
    /// Марки, загруженные из указанного документа ГИС МТ.
    /// </summary>
    Task<List<GisMtMarkEntity>> GetBySourceDocument(string sourceDocumentId, int limit);

    Task<bool> Delete(string id);

    /// <summary>
    /// Поиск марок остатка с пагинацией и отбором по организации, товарной группе, просроченному сроку годности и признаку продажи.
    /// </summary>
    Task<Result<GisMtMarkSearchResult>> Search(
        string searchTerm,
        int page,
        int pageSize,
        string? productGroup = null,
        bool expiredOnly = false,
        bool? sold = null,
        string? organisationInn = null);

    /// <summary>
    /// Товарные группы, по которым у организации есть непроданные марки.
    /// </summary>
    Task<List<string>> GetProductGroups(string organisationInn, int limit);
}
