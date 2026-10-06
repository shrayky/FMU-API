using CouchDb.DatabaseScheme;
using CouchDb.Queries;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Остатки марок ГИС МТ в файле SQLite.
/// </summary>
public class GisMtMarkRepository(
    ILogger<GisMtMarkRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<GisMtMarkEntity, GisMtMarkRow>(logger, context, context.GisMtMarks, appConfiguration, applicationState), IGisMtMarkRepository
{
    private readonly int _queryLimit = appConfiguration.Current().Database.QueryLimit;

    public override string DatabaseName => DatabaseNames.GisMtMarksDbName;

    /// <summary>
    /// Возвращает марку остатка по sGTIN (id документа).
    /// </summary>
    public async Task<GisMtMarkEntity?> Get(string id)
    {
        return await GetById(id);
    }

    /// <summary>
    /// Сохраняет одну марку остатка.
    /// </summary>
    public override async Task<bool> Save(GisMtMarkEntity entity)
    {
        FillId(entity);

        return await base.Save(entity);
    }

    /// <summary>
    /// Сохраняет пакет марок остатка.
    /// </summary>
    public async Task<bool> SaveRange(IEnumerable<GisMtMarkEntity> entities)
    {
        var list = entities.ToList();

        foreach (var entity in list)
            FillId(entity);

        return await SaveBulk(list);
    }

    /// <summary>
    /// Меняет признак продажи марки остатка по sGTIN.
    /// </summary>
    public async Task<Result<GisMtMarkEntity>> ChangeState(string sGtin, bool sold)
    {
        if (!IsOnline())
            return Result.Failure<GisMtMarkEntity>(DatabaseUnavailable);

        var mark = await GetById(sGtin);
        if (mark == null)
            return Result.Failure<GisMtMarkEntity>($"Марка {sGtin} не найдена в остатках ГИС МТ");

        mark.Id = sGtin;
        mark.Sold = sold;

        if (!await Save(mark))
            return Result.Failure<GisMtMarkEntity>($"Не удалось обновить марку {sGtin} в остатках ГИС МТ");

        return Result.Success(mark);
    }

    /// <summary>
    /// Возвращает марки для очистки по сроку хранения и невалидному статусу.
    /// </summary>
    public async Task<List<GisMtMarkEntity>> GetExpiredForCleanup(DateTime olderThanUtc, int limit)
    {
        var nowUtc = DateTime.UtcNow;

        var sold = await ExecuteSafetyDbOperation(
            async () => await _rows
                .Where(row => row.InfoLoadedAt < olderThanUtc && row.Sold)
                .Take(limit)
                .ToListAsync(),
            "GetExpiredForCleanupSold",
            (List<GisMtMarkRow>?)null);

        var marks = ToEntities(sold);

        var expired = await ExecuteSafetyDbOperation(
            async () => await _rows
                .Where(row => row.InfoLoadedAt < olderThanUtc && row.ExpireDate != null && row.ExpireDate < nowUtc)
                .Take(limit)
                .ToListAsync(),
            "GetExpiredForCleanupExpired",
            (List<GisMtMarkRow>?)null);

        foreach (var mark in ToEntities(expired))
        {
            if (marks.All(existing => existing.Id != mark.Id))
                marks.Add(mark);
        }

        return marks.Take(limit).ToList();
    }

    /// <summary>
    /// Удаляет марку остатка по идентификатору.
    /// </summary>
    public override async Task<bool> Delete(string id)
    {
        return await base.Delete(id);
    }

    /// <summary>
    /// Поиск марок остатка с пагинацией и опциональным отбором по товарной группе.
    /// </summary>
    public async Task<Result<GisMtMarkSearchResult>> Search(
        string searchTerm,
        int page,
        int pageSize,
        string? productGroup = null)
    {
        if (!IsOnline())
            return Result.Failure<GisMtMarkSearchResult>(DatabaseUnavailable);

        var hasFilters = !string.IsNullOrWhiteSpace(searchTerm) || !string.IsNullOrWhiteSpace(productGroup);

        var query = _rows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(row => row.SGtin.Contains(searchTerm) || row.Cis.Contains(searchTerm));

        if (!string.IsNullOrWhiteSpace(productGroup))
            query = query.Where(row => row.ProductGroup == productGroup);

        int totalCount;

        if (hasFilters)
        {
            var count = await ExecuteSafetyDbOperation<int?>(
                async () => Math.Min(await query.CountAsync(), ResolveQueryLimit()),
                "SearchCount",
                null);

            if (count == null)
                return Result.Failure<GisMtMarkSearchResult>("Не удалось получить число документов");

            totalCount = count.Value;
        }
        else
        {
            var count = await ExecuteSafetyDbOperation<int?>(
                async () => await _rows.CountAsync(),
                "SearchCountAll",
                null);

            if (count == null)
                return Result.Failure<GisMtMarkSearchResult>("Не удалось получить число документов");

            totalCount = count.Value;
        }

        var rows = await ExecuteSafetyDbOperation(
            async () => await query
                .OrderByDescending(row => row.InfoLoadedAt)
                .ThenBy(row => row.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(),
            "SearchPage",
            (List<GisMtMarkRow>?)null);

        if (rows == null)
            return Result.Failure<GisMtMarkSearchResult>("Ошибка запроса к БД");

        return Result.Success(new GisMtMarkSearchResult
        {
            Marks = ToEntities(rows),
            Count = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            SearchTerm = searchTerm
        });
    }

    protected override GisMtMarkRow CreateRow(GisMtMarkEntity entity, string id) => new()
    {
        Id = id,
        Json = JsonSerializer.Serialize(entity, JsonOptions),
        SGtin = entity.SGtin,
        Cis = entity.Cis,
        ProductGroup = entity.ProductGroup,
        InfoLoadedAt = entity.InfoLoadedAt,
        Sold = entity.Sold,
        ExpireDate = entity.ExpireDate
    };

    private static void FillId(GisMtMarkEntity entity)
    {
        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = !string.IsNullOrEmpty(entity.SGtin) ? entity.SGtin : entity.Cis;
    }

    private int ResolveQueryLimit() => GisMtMarkMangoQueryBuilder.ResolveQueryLimit(_queryLimit);

    private List<GisMtMarkEntity> ToEntities(List<GisMtMarkRow>? rows)
    {
        if (rows == null)
            return [];

        return rows
            .Select(ToEntity)
            .Where(entity => entity != null)
            .Select(entity => entity!)
            .ToList();
    }
}
