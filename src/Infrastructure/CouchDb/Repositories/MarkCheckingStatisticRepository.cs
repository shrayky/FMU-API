using CouchDb.Documents;
using CouchDB.Driver.Types;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories;

public class MarkCheckingStatisticRepository(
    ILogger<MarkCheckingStatisticRepository> logger,
    CouchDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState) : BaseCouchDbRepository<StatisticEntity>(logger, context, context.MarkCheckingStatistic, appConfiguration, applicationState), ICheckStatisticRepository
{
    private static long ToCheckDay(DateTime checkDate) =>
        new DateTimeOffset(DateTime.SpecifyKind(checkDate.Date, DateTimeKind.Utc)).ToUnixTimeSeconds();

    public async Task FailureCheck(string mark, DateTime checkDate)
    {
        StatisticEntity entity = new()
        {
            Id = $"{mark}_{checkDate}",
            CheckDate = checkDate,
            SGtin = mark,
            OnLineCheck = false,
            OffLineCheck = false,
            SuccessCheck = false,
            CheckDay = ToCheckDay(checkDate)
        };

        await CreateAsync(entity);
    }

    public async Task SuccessOffLineCheck(string mark, DateTime checkDate)
    {
        StatisticEntity entity = new()
        {
            Id = $"{mark}_{checkDate}",
            CheckDate = checkDate,
            SGtin = mark,
            OnLineCheck = false,
            OffLineCheck = true,
            SuccessCheck = true,
            WarningMessage = "",
            CheckDay = ToCheckDay(checkDate)
        };

        await CreateAsync(entity);
    }

    public async Task OffLineCheckWithWarnings(string mark, DateTime checkDate, string warningMessage)
    {
        StatisticEntity entity = new()
        {
            Id = $"{mark}_{checkDate}",
            CheckDate = checkDate,
            SGtin = mark,
            OnLineCheck = false,
            OffLineCheck = true,
            SuccessCheck = false,
            WarningMessage = warningMessage,
            CheckDay = ToCheckDay(checkDate)
        };

        await CreateAsync(entity);
    }

    public async Task SuccessOnLineCheck(string mark, DateTime checkDate)
    {
        StatisticEntity entity = new()
        {
            Id = $"{mark}_{checkDate}",
            CheckDate = checkDate,
            SGtin = mark,
            OnLineCheck = true,
            OffLineCheck = false,
            SuccessCheck = true,
            CheckDay = ToCheckDay(checkDate)
        };

        await CreateAsync(entity);
    }

    public async Task OnLineCheckWithWarnings(string mark, DateTime checkDate, string warningMessage)
    {
        StatisticEntity entity = new()
        {
            Id = $"{mark}_{checkDate}",
            CheckDate = checkDate,
            SGtin = mark,
            OnLineCheck = true,
            OffLineCheck = false,
            SuccessCheck = false,
            WarningMessage = warningMessage,
            CheckDay = ToCheckDay(checkDate)
        };

        await CreateAsync(entity);
    }

    public async Task<MarkCheckStatistics> CheckStatisticsByDays(DateTime fromDate, DateTime toDate)
    {
        if (_context == null)
            return new();

        if (!_appState.CouchDbOnline())
            return new();

        var mangoQuery = new
        {
            selector = new Dictionary<string, object>
            {
                ["data.checkDate"] = new Dictionary<string, object>
                {
                    ["$gte"] = fromDate,
                    ["$lte"] = toDate
                }
            },
            limit = await QueryLimitAsync()
        };

        var queryResult = await ExecuteMangoQueryAsync(mangoQuery);
        if (queryResult.IsFailure)
            return new();

        return ToStatistics(queryResult.Value);
    }

    public async Task<MarkCheckStatistics> CheckStatisticsByDay(DateTime checkDate)
    {
        return await CheckStatisticsByDay(ToCheckDay(checkDate));
    }

    public async Task<MarkCheckStatistics> CheckStatisticsByDay(long day)
    {
        if (_context == null)
            return new();

        if (!_appState.CouchDbOnline())
            return new();

        var mangoQuery = new
        {
            selector = new Dictionary<string, object>
            {
                ["data.checkDay"] = day
            },
            limit = await QueryLimitAsync()
        };

        var queryResult = await ExecuteMangoQueryAsync(mangoQuery);
        if (queryResult.IsFailure)
            return new();

        return ToStatistics(queryResult.Value);
    }

    public async Task<CSharpFunctionalExtensions.Result> ClearStorageToDay(DateTime dateToCutStorage, CancellationToken stoppingToken)
    {
        if (_context == null)
            return Result.Failure(DatabaseUnavailable);

        _logger.LogInformation("Начинаю удаление устаревших данных статистики марок до {date}.", dateToCutStorage);

        var mangoQuery = new
        {
            selector = new Dictionary<string, object>
            {
                ["data.checkDate"] = new Dictionary<string, object>
                {
                    ["$lte"] = dateToCutStorage
                }
            },
            limit = 1000
        };

        var data = await ExecuteSafetyDbOperation(
            async () => await _database.QueryAsync(mangoQuery, throwExceptionOnWarning: false, stoppingToken),
            "ClearStorageToDaySelect",
            (List<CouchDoc<StatisticEntity>>?)null);

        if (data == null)
            return Result.Failure(DatabaseUnavailable);

        if (data.Count == 0)
        {
            _logger.LogInformation("Удаление устаревших данных статистики марок завершено - удалять нечего.");
            return Result.Success();
        }

        var operations = new List<BulkItemOperation>();
        foreach (var doc in data)
        {
            var id = !string.IsNullOrWhiteSpace(doc.Id) ? doc.Id : doc.Data?.Id;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(doc.Rev))
                continue;

            operations.Add(BulkItemOperation.Delete(id, doc.Rev));
        }

        if (operations.Count == 0)
        {
            _logger.LogWarning("Найдено {rows} записей статистики, но ни у одной нет Id и Rev для удаления.", data.Count);
            return Result.Failure(DatabaseUnavailable);
        }

        _logger.LogInformation("Удаляю {rows} записей из статистики.", operations.Count);

        var deleted = await ExecuteSafetyDbOperation(
            async () => await _database.ExecuteBulkItemOperationsAsync(operations, stoppingToken),
            "ClearStorageToDayDelete");

        if (!deleted)
            return Result.Failure(DatabaseUnavailable);

        _logger.LogInformation("Удаление устаревших данных статистики марок завершено.");
        return Result.Success();
    }

    /// <summary>
    /// Собирает агрегаты по списку записей статистики.
    /// </summary>
    private static MarkCheckStatistics ToStatistics(List<StatisticEntity> marks) => new()
    {
        Total = marks.Count,
        SuccessfulOnlineChecks = marks.Count(m => m.SuccessCheck && m.OnLineCheck),
        SuccessfulOfflineChecks = marks.Count(m => m.SuccessCheck && m.OffLineCheck)
    };

    /// <summary>
    /// Возвращает лимит выборки из настроек БД.
    /// </summary>
    private async Task<int> QueryLimitAsync()
    {
        var appConfig = await _appConfiguration.CurrentAsync();
        return appConfig.Database.QueryLimit == 0 ? 1000000 : appConfig.Database.QueryLimit;
    }
}
