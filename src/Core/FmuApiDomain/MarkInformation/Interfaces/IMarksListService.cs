using CSharpFunctionalExtensions;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.MarkInformation.Interfaces;

public interface IMarksListService
{
    Task<Result<MarkSearchResult>> List(string searchTerm, int page, int pageSize);

    Task<Result<StatisticEntity>> CheckInformation(string id);
}
