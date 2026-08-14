using CSharpFunctionalExtensions;
using FmuApiDomain.Statistics.Entities;
using FmuApiDomain.Mark.Models;

namespace FmuApiDomain.Mark.Interfaces;

public interface IMarksListService
{
    Task<Result<MarkSearchResult>> List(string searchTerm, int page, int pageSize);

    Task<Result<StatisticEntity>> CheckInformation(string id);
}
