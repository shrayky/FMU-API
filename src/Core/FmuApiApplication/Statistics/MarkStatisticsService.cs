using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Statistics;

[AutoRegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped)]
public class MarkStatisticsService : IMarkStatisticsService
{
    public const string DatabaseDisabled = "База данных отключена";
    public const string DatabaseUnavailable = "База данных недоступна в данный момент";

    private readonly ILogger<MarkStatisticsService> _logger;
    private readonly ICheckStatisticRepository _repository;
    private readonly IParametersService _parametersService;
    private readonly IApplicationState _appState;
    private readonly IMemoryCache _cache;

    private readonly Parameters _parameters;

    public MarkStatisticsService(
        ILogger<MarkStatisticsService> logger,
        ICheckStatisticRepository repository,
        IParametersService parametersService,
        IApplicationState appState,
        IMemoryCache cache)
    {
        _logger = logger;
        _repository = repository;
        _parametersService = parametersService;
        _appState = appState;
        _cache = cache;

        _parameters = parametersService.Current();
    }

    public async Task<MarkCheckStatistics> ByDays(DateTime fromDate, DateTime toDate)
    {
        if (!_parameters.Database.Enable)
            return new MarkCheckStatistics();

        var statistics = await _repository.CheckStatisticsByDays(fromDate, toDate);

        return statistics;
    }

    public async Task<MarkCheckStatistics> LastWeek()
    {
        var toDate = DateTime.Now.Date.AddDays(-1);
        var fromDate = DateTime.Now.AddDays(-8).Date;

        return await ByDays(fromDate, toDate);
    }

    public async Task<MarkCheckStatistics> LastMonth()
    {
        var toDate = DateTime.Now.Date.AddDays(-1);
        var fromDate = DateTime.Now.AddDays(-31).Date;

        return await ByDays(fromDate, toDate);
    }

    public async Task<MarkCheckStatistics> Today()
    {
        var toDate = DateTime.Now.Date.AddDays(1);
        var fromDate = DateTime.Today;

        return await ByDays(fromDate, toDate);
    }

    public async Task<MarkCheckStatistics> ByDay(DateTime day)
    {
        var dayInUnixTime = new DateTimeOffset(DateTime.SpecifyKind(day.Date, DateTimeKind.Utc)).ToUnixTimeSeconds();
        return await _repository.CheckStatisticsByDay(dayInUnixTime);
    }

    public async Task<Result> ClearAll(CancellationToken cancellationToken)
    {
        var unavailable = await DatabaseError();
        if (unavailable != null)
            return Result.Failure(unavailable);

        _logger.LogInformation("Запрошена полная очистка базы статистики.");

        var result = await _repository.ClearAll(cancellationToken);
        if (result.IsFailure)
            return result;

        _cache.Remove(CachedMarkStatisticsProvider.Key7days);
        _cache.Remove(CachedMarkStatisticsProvider.Key30days);

        return Result.Success();
    }

    private async Task<string?> DatabaseError()
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return DatabaseDisabled;

        if (!_appState.CouchDbOnline())
            return DatabaseUnavailable;

        return null;
    }
}