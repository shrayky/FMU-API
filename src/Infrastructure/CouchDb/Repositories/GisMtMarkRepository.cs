using CouchDb.Queries;
using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories;

public class GisMtMarkRepository(
    ILogger<GisMtMarkRepository> logger,
    CouchDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState) : BaseCouchDbRepository<GisMtMarkEntity>(logger, context, context.GisMtMarks, appConfiguration, applicationState), IGisMtMarkRepository
{

    /// <summary>
    /// Возвращает марку остатка по sGTIN (id документа).
    /// </summary>
    public async Task<GisMtMarkEntity?> Get(string id)
    {
        if (_context == null)
            return null;

        return await GetByIdAsync(id);
    }

    /// <summary>
    /// Сохраняет одну марку остатка.
    /// </summary>
    public async Task<bool> Save(GisMtMarkEntity entity)
    {
        if (_context == null)
            return false;

        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = !string.IsNullOrEmpty(entity.SGtin) ? entity.SGtin : entity.Cis;

        var existing = await GetByIdAsync(entity.Id);
        if (existing == null)
            return await CreateAsync(entity);

        return await UpdateAsync(entity.Id, entity);
    }

    /// <summary>
    /// Сохраняет пакет марок остатка.
    /// </summary>
    public async Task<bool> SaveRange(IEnumerable<GisMtMarkEntity> entities)
    {
        if (_context == null)
            return false;

        var list = entities.ToList();
        foreach (var entity in list)
        {
            if (string.IsNullOrEmpty(entity.Id))
                entity.Id = !string.IsNullOrEmpty(entity.SGtin) ? entity.SGtin : entity.Cis;
        }

        return await CreateBulkAsync(list);
    }

    /// <summary>
    /// Меняет признак продажи марки остатка по sGTIN.
    /// </summary>
    public async Task<Result<GisMtMarkEntity>> ChangeState(string sGtin, bool sold)
    {
        if (_context == null)
            return Result.Failure<GisMtMarkEntity>(DatabaseUnavailable);

        if (!_appState.CouchDbOnline())
            return Result.Failure<GisMtMarkEntity>(DatabaseUnavailable);

        var mark = await GetByIdAsync(sGtin);
        if (mark == null)
            return Result.Failure<GisMtMarkEntity>($"Марка {sGtin} не найдена в остатках ГИС МТ");

        mark.Sold = sold;

        if (!await UpdateAsync(sGtin, mark))
            return Result.Failure<GisMtMarkEntity>($"Не удалось обновить марку {sGtin} в остатках ГИС МТ");

        return Result.Success(mark);
    }

    /// <summary>
    /// Возвращает марки для очистки по сроку хранения и невалидному статусу.
    /// </summary>
    public async Task<List<GisMtMarkEntity>> GetExpiredForCleanup(DateTime olderThanUtc, int limit)
    {
        if (_context == null)
            return [];

        var query = new
        {
            selector = new
            {
                data = new
                {
                    infoLoadedAt = new Dictionary<string, object>
                    {
                        ["$lt"] = olderThanUtc
                    },
                    sold = true
                }
            },
            limit
        };

        var soldResult = await ExecuteMangoQueryAsync(query);
        var marks = soldResult.IsSuccess ? soldResult.Value : [];

        var expiredQuery = new
        {
            selector = new
            {
                data = new
                {
                    infoLoadedAt = new Dictionary<string, object>
                    {
                        ["$lt"] = olderThanUtc
                    },
                    expireDate = new Dictionary<string, object>
                    {
                        ["$lt"] = DateTime.UtcNow,
                        ["$ne"] = null!
                    }
                }
            },
            limit
        };

        var expiredResult = await ExecuteMangoQueryAsync(expiredQuery);
        if (expiredResult.IsSuccess)
        {
            foreach (var mark in expiredResult.Value)
            {
                if (marks.All(m => m.Id != mark.Id))
                    marks.Add(mark);
            }
        }

        return marks.Take(limit).ToList();
    }

    /// <summary>
    /// Удаляет марку остатка по идентификатору.
    /// </summary>
    public async Task<bool> Delete(string id)
    {
        if (_context == null)
            return false;

        return await base.DeleteAsync(id);
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
        if (_context == null)
            return Result.Failure<GisMtMarkSearchResult>(DatabaseUnavailable);

        if (!_appState.CouchDbOnline())
            return Result.Failure<GisMtMarkSearchResult>(DatabaseUnavailable);

        return await QueryWithPagination(searchTerm, productGroup, page, pageSize);
    }

    private async Task<Result<GisMtMarkSearchResult>> QueryWithPagination(
        string searchTerm,
        string? productGroup,
        int page,
        int pageSize)
    {
        var hasFilters = !string.IsNullOrWhiteSpace(searchTerm) || !string.IsNullOrWhiteSpace(productGroup);

        int totalCount;
        if (!hasFilters)
        {
            var documentsCount = await GetDocumentsCountAsync();
            if (documentsCount == null)
                return Result.Failure<GisMtMarkSearchResult>("Не удалось получить число документов");

            totalCount = documentsCount.Value;
        }
        else
        {
            var countQuery = GisMtMarkMangoQueryBuilder.BuildCountQuery(
                searchTerm,
                productGroup,
                await QueryLimitAsync());

            var countResult = await ExecuteMangoCountAsync(countQuery);
            if (countResult.IsFailure)
                return Result.Failure<GisMtMarkSearchResult>(countResult.Error);

            totalCount = countResult.Value;
        }

        var pageQuery = GisMtMarkMangoQueryBuilder.BuildPageQuery(searchTerm, productGroup, page, pageSize);
        var pageResult = await ExecuteMangoQueryAsync(pageQuery);
        if (pageResult.IsFailure)
            return Result.Failure<GisMtMarkSearchResult>(pageResult.Error);

        return Result.Success(new GisMtMarkSearchResult
        {
            Marks = pageResult.Value,
            Count = totalCount,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            SearchTerm = searchTerm
        });
    }

    private async Task<int> QueryLimitAsync()
    {
        var appConfig = await _appConfiguration.CurrentAsync();
        return GisMtMarkMangoQueryBuilder.ResolveQueryLimit(appConfig.Database.QueryLimit);
    }
}
