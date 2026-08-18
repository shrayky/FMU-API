using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.ProductGroups.Entities;
using FmuApiDomain.ProductGroups.Interfaces;
using FmuApiDomain.ProductGroups.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories;

public class GtinCatalogRepository(
    ILogger<GtinCatalogRepository> logger,
    CouchDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseCouchDbRepository<GtinCatalogEntity>(logger, context, context.GtinCatalog, appConfiguration, applicationState),
      IGtinCatalogRepository
{
    public async Task<GtinCatalogEntity?> Get(string gtin)
    {
        if (_context == null)
            return null;

        if (string.IsNullOrWhiteSpace(gtin))
            return null;

        return await GetByIdAsync(gtin);
    }

    public async Task<bool> Save(GtinCatalogEntity entity)
    {
        if (_context == null)
            return false;

        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = entity.Gtin;

        var existing = await GetByIdAsync(entity.Id);
        if (existing == null)
            return await CreateAsync(entity);

        return await UpdateAsync(entity.Id, entity);
    }

    public async Task<GtinCatalogSearchResult> Search(string searchTerm, int page, int pageSize)
    {
        if (_context == null || !_appState.CouchDbOnline())
            return new GtinCatalogSearchResult { CurrentPage = page, PageSize = pageSize };

        var selector = BuildSelector(searchTerm);
        var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);

        if (!hasSearch)
        {
            var totalCount = await GetDocumentsCountAsync();

            if (totalCount == null)
                return new GtinCatalogSearchResult { CurrentPage = page, PageSize = pageSize };

            var mangoQuery = new
            {
                selector,
                limit = pageSize,
                skip = (page - 1) * pageSize
            };

            var pageResult = await ExecuteMangoQueryAsync(mangoQuery);
            if (pageResult.IsFailure)
                return new GtinCatalogSearchResult { CurrentPage = page, PageSize = pageSize };

            return new GtinCatalogSearchResult
            {
                Items = pageResult.Value,
                Count = totalCount.Value,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount.Value / pageSize),
                SearchTerm = string.Empty
            };
        }

        var searchQuery = new { selector };
        var allResults = await ExecuteMangoQueryAsync(searchQuery);
        if (allResults.IsFailure)
            return new GtinCatalogSearchResult { CurrentPage = page, PageSize = pageSize, SearchTerm = searchTerm };

        var paginated = allResults.Value
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new GtinCatalogSearchResult
        {
            Items = paginated,
            Count = allResults.Value.Count,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)allResults.Value.Count / pageSize),
            SearchTerm = searchTerm
        };
    }

    private static Dictionary<string, object> BuildSelector(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new Dictionary<string, object>
            {
                ["data"] = new Dictionary<string, object> { ["$exists"] = true }
            };
        }

        return new Dictionary<string, object>
        {
            ["data.gtin"] = new Dictionary<string, object> { ["$regex"] = searchTerm }
        };
    }
}
