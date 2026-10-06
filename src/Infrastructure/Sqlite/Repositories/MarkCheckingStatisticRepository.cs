using CouchDb.DatabaseScheme;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Mark.Models;
using FmuApiDomain.Statistics.Entities;
using FmuApiDomain.Statistics.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Статистика проверок марок в файле SQLite.
/// </summary>
public class MarkCheckingStatisticRepository(
    ILogger<MarkCheckingStatisticRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<StatisticEntity, MarkCheckingStatisticRow>(logger, context, context.MarkCheckingStatistic, appConfiguration, applicationState), ICheckStatisticRepository
{
    private const int DeleteBatchSize = 1000;
    private const int DefaultQueryLimit = 1000000;
    private const int LastCheckCandidateLimit = 5;

    private readonly int _queryLimit = appConfiguration.Current().Database.QueryLimit;

    public override string DatabaseName => DatabaseNames.MarkCheckingStatistic;

    public async Task Add(StatisticEntity entity)
    {
        if (entity.CheckDay == 0)
            entity.CheckDay = ToCheckDay(entity.CheckDate);

        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = $"{entity.SGtin}_{entity.CheckDate}";

        await Save(entity);
    }

    public async Task<StatisticEntity?> ById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (!IsOnline())
            return null;

        return await GetById(id);
    }

    public async Task<Dictionary<string, LastMarkCheck>> LastChecks(IReadOnlyList<string> sgtins)
    {
        var result = new Dictionary<string, LastMarkCheck>(StringComparer.Ordinal);
        if (sgtins.Count == 0)
            return result;

        if (!IsOnline())
            return result;

        var distinct = sgtins
            .Where(sgtin => !string.IsNullOrEmpty(sgtin))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (distinct.Count == 0)
            return result;

        foreach (var sgtin in distinct)
        {
            var check = await FindLastCheck(sgtin);

            if (!check.Ok)
                return result;

            Remember(result, check.Check);
        }

        return result;
    }

    public async Task<MarkCheckStatistics> CheckStatisticsByDays(DateTime fromDate, DateTime toDate)
    {
        if (!IsOnline())
            return new();

        var rows = await ExecuteSafetyDbOperation(
            async () => await _rows
                .Where(row => row.CheckDate >= fromDate && row.CheckDate <= toDate)
                .Take(QueryLimit())
                .ToListAsync(),
            "CheckStatisticsByDays",
            (List<MarkCheckingStatisticRow>?)null);

        if (rows == null)
            return new();

        return ToStatistics(rows);
    }

    public async Task<MarkCheckStatistics> CheckStatisticsByDay(DateTime checkDate)
    {
        return await CheckStatisticsByDay(ToCheckDay(checkDate));
    }

    public async Task<MarkCheckStatistics> CheckStatisticsByDay(long day)
    {
        if (!IsOnline())
            return new();

        var rows = await ExecuteSafetyDbOperation(
            async () => await _rows
                .Where(row => row.CheckDay == day)
                .Take(QueryLimit())
                .ToListAsync(),
            "CheckStatisticsByDay",
            (List<MarkCheckingStatisticRow>?)null);

        if (rows == null)
            return new();

        return ToStatistics(rows);
    }

    public async Task<Result> ClearStorageToDay(DateTime dateToCutStorage, CancellationToken stoppingToken)
    {
        if (!IsOnline())
            return Result.Failure(DatabaseUnavailable);

        _logger.LogInformation("Начинаю удаление устаревших данных статистики марок до {date}.", dateToCutStorage);

        var deleted = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            var rows = await _rows
                .Where(row => row.CheckDate <= dateToCutStorage)
                .Take(DeleteBatchSize)
                .ToListAsync(stoppingToken);

            if (rows.Count == 0)
                break;

            _rows.RemoveRange(rows);
            await _context.SaveChangesAsync(stoppingToken);

            deleted += rows.Count;
        }

        if (deleted == 0)
            _logger.LogInformation("Удаление устаревших данных статистики марок завершено - удалять нечего.");
        else
            _logger.LogInformation("Удаление устаревших данных статистики марок завершено. Удалено {rows} записей.", deleted);

        return Result.Success();
    }

    public async Task<Result> ClearAll(CancellationToken cancellationToken)
    {
        if (!IsOnline())
            return Result.Failure(DatabaseUnavailable);

        _logger.LogInformation("Начинаю полную очистку базы статистики марок.");

        var totalDeleted = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var rows = await _rows.Take(DeleteBatchSize).ToListAsync(cancellationToken);

            if (rows.Count == 0)
                break;

            _rows.RemoveRange(rows);
            await _context.SaveChangesAsync(cancellationToken);

            totalDeleted += rows.Count;
            _logger.LogInformation("Удаляю {rows} записей из статистики.", rows.Count);
        }

        _logger.LogInformation("Полная очистка базы статистики марок завершена. Удалено {total} записей.", totalDeleted);

        return Result.Success();
    }

    protected override MarkCheckingStatisticRow CreateRow(StatisticEntity entity, string id) => new()
    {
        Id = id,
        Json = JsonSerializer.Serialize(entity, JsonOptions),
        SGtin = entity.SGtin,
        CheckDay = entity.CheckDay,
        CheckDate = entity.CheckDate
    };

    private async Task<(bool Ok, KeyValuePair<string, LastMarkCheck>? Check)> FindLastCheck(string sgtin)
    {
        var rows = await ExecuteSafetyDbOperation(
            async () => await _rows
                .Where(row => row.SGtin == sgtin)
                .OrderByDescending(row => row.CheckDate)
                .Take(LastCheckCandidateLimit)
                .ToListAsync(),
            "FindLastCheck",
            (List<MarkCheckingStatisticRow>?)null);

        if (rows == null)
            return (false, null);

        var last = rows
            .Select(ToEntity)
            .Where(entity => entity != null && HasCheckPayload(entity))
            .OrderByDescending(entity => entity!.CheckDate)
            .FirstOrDefault();

        if (last == null || string.IsNullOrEmpty(last.Id))
            return (true, null);

        var check = new KeyValuePair<string, LastMarkCheck>(sgtin, new LastMarkCheck
        {
            Id = last.Id,
            CheckSource = last.CheckSource
        });

        return (true, check);
    }

    private int QueryLimit() => _queryLimit == 0 ? DefaultQueryLimit : _queryLimit;

    private static long ToCheckDay(DateTime checkDate) =>
        new DateTimeOffset(DateTime.SpecifyKind(checkDate.Date, DateTimeKind.Utc)).ToUnixTimeSeconds();

    private static void Remember(
        Dictionary<string, LastMarkCheck> result,
        KeyValuePair<string, LastMarkCheck>? check)
    {
        if (check == null)
            return;

        result[check.Value.Key] = check.Value.Value;
    }

    private static bool HasCheckPayload(StatisticEntity entity) =>
        entity.CheckRequest != null || entity.CheckResponse != null;

    private static MarkCheckStatistics ToStatistics(IEnumerable<MarkCheckingStatisticRow> rows)
    {
        var marks = rows
            .Select(row => DeserializeStatistic(row))
            .Where(entity => entity != null)
            .Select(entity => entity!)
            .ToList();

        return new MarkCheckStatistics
        {
            Total = marks.Count,
            SuccessfulOnlineChecks = marks.Count(mark => mark.SuccessCheck && mark.OnLineCheck),
            SuccessfulOfflineChecks = marks.Count(mark => mark.SuccessCheck && mark.OffLineCheck)
        };
    }

    private static StatisticEntity? DeserializeStatistic(MarkCheckingStatisticRow row)
    {
        try
        {
            return JsonSerializer.Deserialize<StatisticEntity>(row.Json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
