using CouchDb.Documents;
using CouchDB.Driver.Types;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Statistics.Entities;
using FmuApiDomain.Mark.Models;
using FmuApiDomain.Statistics.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories;

public class MarkCheckingStatisticRepository(
    ILogger<MarkCheckingStatisticRepository> logger,
    CouchDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState) : BaseCouchDbRepository<StatisticEntity>(logger, context, context.MarkCheckingStatistic, appConfiguration, applicationState), ICheckStatisticRepository
{
    private const int DeleteBatchSize = 1000;

    private static long ToCheckDay(DateTime checkDate) =>
        new DateTimeOffset(DateTime.SpecifyKind(checkDate.Date, DateTimeKind.Utc)).ToUnixTimeSeconds();

    public async Task Add(StatisticEntity entity)
    {
        if (entity.CheckDay == 0)
            entity.CheckDay = ToCheckDay(entity.CheckDate);

        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = $"{entity.SGtin}_{entity.CheckDate}";

        await CreateAsync(entity);
    }

    public async Task<StatisticEntity?> ById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (_context == null)
            return null;

        if (!_appState.CouchDbOnline())
            return null;

        return await GetByIdAsync(id);
    }

    public async Task<Dictionary<string, string>> LastCheckIds(IReadOnlyList<string> sgtins)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (sgtins.Count == 0)
            return result;

        if (_context == null || !_appState.CouchDbOnline())
            return result;

        var mangoQuery = new
        {
            selector = new Dictionary<string, object>
            {
                ["data.sGtin"] = new Dictionary<string, object>
                {
                    ["$in"] = sgtins.ToList()
                }
            },
            limit = await QueryLimitAsync()
        };

        var queryResult = await ExecuteMangoQueryAsync(mangoQuery);
        if (queryResult.IsFailure)
            return result;

        foreach (var group in queryResult.Value.GroupBy(x => x.SGtin))
        {
            var last = group
                .Where(HasCheckPayload)
                .OrderByDescending(x => x.CheckDate)
                .FirstOrDefault();

            if (last == null || string.IsNullOrEmpty(last.Id))
                continue;

            result[group.Key] = last.Id;
        }

        return result;
    }

    private static bool HasCheckPayload(StatisticEntity entity) =>
        entity.CheckRequest != null || entity.CheckResponse != null;

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

        var mangoQuery = DocumentsByCheckDateQuery(new Dictionary<string, object>
        {
            ["$lte"] = dateToCutStorage
        });

        var deleted = await DeleteDocumentsBatch(mangoQuery, "ClearStorageToDay", stoppingToken);
        if (deleted.IsFailure)
            return Result.Failure(deleted.Error);

        if (deleted.Value == 0)
            _logger.LogInformation("Удаление устаревших данных статистики марок завершено - удалять нечего.");
        else
            _logger.LogInformation("Удаление устаревших данных статистики марок завершено. Удалено {rows} записей.", deleted.Value);

        return Result.Success();
    }

    /// <summary>
    /// Удаляет все документы статистики пачками, пока база не опустеет.
    /// </summary>
    public async Task<Result> ClearAll(CancellationToken cancellationToken)
    {
        if (_context == null)
            return Result.Failure(DatabaseUnavailable);

        _logger.LogInformation("Начинаю полную очистку базы статистики марок.");

        var totalDeleted = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var mangoQuery = new
            {
                selector = new Dictionary<string, object>
                {
                    ["_id"] = new Dictionary<string, object>
                    {
                        ["$exists"] = true
                    }
                },
                limit = DeleteBatchSize
            };

            var deleted = await DeleteDocumentsBatch(mangoQuery, "ClearAll", cancellationToken);
            if (deleted.IsFailure)
                return Result.Failure(deleted.Error);

            if (deleted.Value == 0)
                break;

            totalDeleted += deleted.Value;
            _logger.LogInformation("Удаляю {rows} записей из статистики.", deleted.Value);
        }

        _logger.LogInformation("Полная очистка базы статистики марок завершена. Удалено {total} записей.", totalDeleted);
        return Result.Success();
    }

    private static object DocumentsByCheckDateQuery(Dictionary<string, object> checkDateFilter) => new
    {
        selector = new Dictionary<string, object>
        {
            ["data.checkDate"] = checkDateFilter
        },
        limit = DeleteBatchSize
    };

    /// <summary>
    /// Выбирает пачку документов по mango-запросу и удаляет их bulk-операцией.
    /// </summary>
    private async Task<Result<int>> DeleteDocumentsBatch(object mangoQuery, string operationName, CancellationToken stoppingToken)
    {
        if (_context == null)
            return Result.Failure<int>(DatabaseUnavailable);

        var data = await ExecuteSafetyDbOperation(
            async () => await _database.QueryAsync(mangoQuery, throwExceptionOnWarning: false, stoppingToken),
            $"{operationName}Select",
            (List<CouchDoc<StatisticEntity>>?)null);

        if (data == null)
            return Result.Failure<int>(DatabaseUnavailable);

        if (data.Count == 0)
            return Result.Success(0);

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
            return Result.Failure<int>(DatabaseUnavailable);
        }

        var deleted = await ExecuteSafetyDbOperation(
            async () => await _database.ExecuteBulkItemOperationsAsync(operations, stoppingToken),
            $"{operationName}Delete");

        if (!deleted)
            return Result.Failure<int>(DatabaseUnavailable);

        return Result.Success(operations.Count);
    }

    private static MarkCheckStatistics ToStatistics(List<StatisticEntity> marks) => new()
    {
        Total = marks.Count,
        SuccessfulOnlineChecks = marks.Count(m => m.SuccessCheck && m.OnLineCheck),
        SuccessfulOfflineChecks = marks.Count(m => m.SuccessCheck && m.OffLineCheck)
    };

    private async Task<int> QueryLimitAsync()
    {
        var appConfig = await _appConfiguration.CurrentAsync();
        return appConfig.Database.QueryLimit == 0 ? 1000000 : appConfig.Database.QueryLimit;
    }
}
