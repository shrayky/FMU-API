using CouchDb.DatabaseScheme;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.ProductGroups.Entities;
using FmuApiDomain.ProductGroups.Interfaces;
using FmuApiDomain.ProductGroups.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sqlite.Documents;
using System.Text.Json;

namespace Sqlite.Repositories;

/// <summary>
/// Каталог GTIN в файле SQLite.
/// </summary>
public class GtinCatalogRepository(
    ILogger<GtinCatalogRepository> logger,
    SqliteDbContext context,
    IParametersService appConfiguration,
    IApplicationState applicationState)
    : BaseSqliteRepository<GtinCatalogEntity, GtinCatalogRow>(logger, context, context.GtinCatalog, appConfiguration, applicationState), IGtinCatalogRepository
{
    public override string DatabaseName => DatabaseNames.GtinCatalogDbName;

    public async Task<GtinCatalogEntity?> Get(string gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
            return null;

        return await GetById(gtin);
    }

    public override async Task<bool> Save(GtinCatalogEntity entity)
    {
        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = entity.Gtin;

        return await base.Save(entity);
    }

    public async Task<GtinCatalogSearchResult> Search(string searchTerm, int page, int pageSize)
    {
        var emptyResult = new GtinCatalogSearchResult { CurrentPage = page, PageSize = pageSize };

        if (!IsOnline())
            return emptyResult;

        var query = _rows.AsQueryable();
        var hasSearch = !string.IsNullOrWhiteSpace(searchTerm);

        if (hasSearch)
            query = query.Where(row => row.Gtin.Contains(searchTerm));

        var totalCount = await ExecuteSafetyDbOperation<int?>(
            async () => await query.CountAsync(),
            "SearchCount",
            null);

        if (totalCount == null)
            return emptyResult;

        var rows = await ExecuteSafetyDbOperation(
            async () => await query
                .OrderBy(row => row.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(),
            "SearchPage",
            (List<GtinCatalogRow>?)null);

        if (rows == null)
            return new GtinCatalogSearchResult { CurrentPage = page, PageSize = pageSize, SearchTerm = searchTerm };

        return new GtinCatalogSearchResult
        {
            Items = rows
                .Select(ToEntity)
                .Where(entity => entity != null)
                .Select(entity => entity!)
                .ToList(),
            Count = totalCount.Value,
            CurrentPage = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount.Value / pageSize),
            SearchTerm = hasSearch ? searchTerm : string.Empty
        };
    }

    protected override GtinCatalogRow CreateRow(GtinCatalogEntity entity, string id) => new()
    {
        Id = id,
        Json = JsonSerializer.Serialize(entity, JsonOptions),
        Gtin = entity.Gtin
    };
}
