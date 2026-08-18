using FmuApiDomain.Attributes;
using FmuApiDomain.ProductGroups.Entities;
using FmuApiDomain.ProductGroups.Interfaces;
using FmuApiDomain.ProductGroups.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.ProductGroups;

/// <summary>
/// Каталог GTIN: код группы Честного знака. Пишется только после online-проверки.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GtinCatalogService(
    IGtinCatalogRepository gtinCatalogRepository,
    IMemoryCache memoryCache,
    IApplicationState applicationState,
    ILogger<GtinCatalogService> logger) : IGtinCatalogService
{
    private readonly IGtinCatalogRepository _gtinCatalogRepository = gtinCatalogRepository;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly IApplicationState _applicationState = applicationState;
    private readonly ILogger<GtinCatalogService> _logger = logger;

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);

    /// <summary>
    /// Возвращает запись каталога по GTIN (память, затем Couch).
    /// </summary>
    public async Task<GtinCatalogEntity?> Get(string gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
            return null;

        if (_memoryCache.TryGetValue(CacheKey(gtin), out GtinCatalogEntity? cached) && cached != null)
            return cached;

        if (!_applicationState.CouchDbOnline())
            return null;

        var fromDb = await _gtinCatalogRepository.Get(gtin);
        if (fromDb != null)
            _memoryCache.Set(CacheKey(gtin), fromDb, CacheLifetime);

        return fromDb;
    }

    /// <summary>
    /// Сохраняет GTIN после успешной online-проверки.
    /// </summary>
    public async Task SaveFromOnlineCheck(string gtin, int trueApiGroupId)
    {
        if (string.IsNullOrWhiteSpace(gtin) || trueApiGroupId <= 0)
            return;

        var entity = new GtinCatalogEntity
        {
            Id = gtin,
            Gtin = gtin,
            TrueApiGroupId = trueApiGroupId
        };

        _memoryCache.Set(CacheKey(gtin), entity, CacheLifetime);

        if (!_applicationState.CouchDbOnline())
            return;

        var saved = await _gtinCatalogRepository.Save(entity);
        if (!saved)
            _logger.LogWarning("Не удалось сохранить GTIN {Gtin} в каталог", gtin);
    }

    /// <summary>
    /// Ищет записи каталога GTIN с пагинацией.
    /// </summary>
    public async Task<GtinCatalogSearchResult> Search(string searchTerm, int page, int pageSize)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1 || pageSize > 100)
            pageSize = 50;

        if (!_applicationState.CouchDbOnline())
            return new GtinCatalogSearchResult { CurrentPage = page, PageSize = pageSize };

        return await _gtinCatalogRepository.Search(searchTerm ?? string.Empty, page, pageSize);
    }

    private static string CacheKey(string gtin) => $"gtin-catalog:{gtin}";
}
