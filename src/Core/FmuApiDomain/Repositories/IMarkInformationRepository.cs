using CSharpFunctionalExtensions;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.Repositories
{
    public interface IMarkInformationRepository
    {
        Task<MarkEntity> GetAsync(string id);
        Task<MarkEntity> SetStateAsync(string id, string state, SaleData saleData);
        Task<MarkEntity> AddAsync(MarkEntity mark);
        Task<List<MarkEntity>> GetDocumentsAsync(List<string> gtins);
        Task<bool> AddRangeAsync(List<MarkEntity> markEntities);
        Task<Result<MarkSearchResult>> SearchMarkData(string searchTerm, int page, int pageSize);
    }
}
