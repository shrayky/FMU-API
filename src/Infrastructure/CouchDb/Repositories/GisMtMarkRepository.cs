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
        var selector = BuildSelector(searchTerm, productGroup);
        var hasFilters = !string.IsNullOrWhiteSpace(searchTerm) || !string.IsNullOrWhiteSpace(productGroup);

        if (!hasFilters)
        {
            var totalCount = await ExecuteSafetyDbOperation(
                async () =>
                {
                    var dbInfo = await _database.GetInfoAsync();
                    return (int?)dbInfo.DocCount;
                },
                "GisMtMarksGetInfo",
                (int?)null);

            if (totalCount == null)
                return Result.Failure<GisMtMarkSearchResult>("Не удалось получить число документов");

            var mangoQuery = new
            {
                selector,
                sort = new[] { new Dictionary<string, string> { ["data.infoLoadedAt"] = "desc" } },
                limit = pageSize,
                skip = (page - 1) * pageSize
            };

            var pageResult = await ExecuteMangoQueryAsync(mangoQuery);
            if (pageResult.IsFailure)
                return Result.Failure<GisMtMarkSearchResult>(pageResult.Error);

            return Result.Success(new GisMtMarkSearchResult
            {
                Marks = pageResult.Value,
                Count = totalCount.Value,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount.Value / pageSize),
                SearchTerm = string.Empty
            });
        }

        var searchQuery = new
        {
            selector,
            sort = new[] { new Dictionary<string, string> { ["data.infoLoadedAt"] = "desc" } }
        };

        var allResults = await ExecuteMangoQueryAsync(searchQuery);
        if (allResults.IsFailure)
            return Result.Failure<GisMtMarkSearchResult>(allResults.Error);

        var paginated = allResults.Value
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result.Success(new GisMtMarkSearchResult
        {
            Marks = paginated,
            Count = allResults.Value.Count,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)allResults.Value.Count / pageSize),
            SearchTerm = searchTerm
        });
    }

    /// <summary>
    /// Собирает mango-селектор по строке поиска и товарной группе.
    /// </summary>
    private static Dictionary<string, object> BuildSelector(string searchTerm, string? productGroup)
    {
        var conditions = new List<object>();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            conditions.Add(new Dictionary<string, object>
            {
                ["$or"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["data.sGtin"] = new Dictionary<string, object> { ["$regex"] = searchTerm }
                    },
                    new Dictionary<string, object>
                    {
                        ["data.cis"] = new Dictionary<string, object> { ["$regex"] = searchTerm }
                    }
                }
            });
        }

        if (!string.IsNullOrWhiteSpace(productGroup))
        {
            conditions.Add(new Dictionary<string, object>
            {
                ["data.productGroup"] = productGroup
            });
        }

        if (conditions.Count == 0)
        {
            return new Dictionary<string, object>
            {
                ["data"] = new Dictionary<string, object> { ["$exists"] = true }
            };
        }

        if (conditions.Count == 1)
            return (Dictionary<string, object>)conditions[0];

        return new Dictionary<string, object>
        {
            ["$and"] = conditions.ToArray()
        };
    }
}
