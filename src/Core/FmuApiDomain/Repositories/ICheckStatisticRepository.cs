using CSharpFunctionalExtensions;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.MarkInformation.Models;

namespace FmuApiDomain.Repositories;

public interface ICheckStatisticRepository
{
    Task Add(StatisticEntity entity);
    
    Task<StatisticEntity?> ById(string id);
    
    Task<Dictionary<string, string>> LastCheckIds(IReadOnlyList<string> sgtins);
    
    Task<MarkCheckStatistics> CheckStatisticsByDays(DateTime fromDate, DateTime toDate);
    
    Task<MarkCheckStatistics> CheckStatisticsByDay(long day);
    
    Task<Result> ClearStorageToDay(DateTime day, CancellationToken stoppingToken);

    Task<Result> ClearAll(CancellationToken cancellationToken);
}
