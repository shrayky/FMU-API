using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.ProductGroups.Interfaces;
using FmuApiDomain.TrueApi.MarkData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.ProductGroups;

/// <summary>
/// Определяет код товарной группы Честного знака для запроса в локальный модуль.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class ProductGroupResolver(
    IParametersService parametersService,
    IGtinCatalogService gtinCatalogService,
    ILogger<ProductGroupResolver> logger) : IProductGroupResolver
{
    private readonly IParametersService _parametersService = parametersService;
    private readonly IGtinCatalogService _gtinCatalogService = gtinCatalogService;
    private readonly ILogger<ProductGroupResolver> _logger = logger;

    public async Task<int?> ResolveAsync(int atolItemType, string gtin)
    {
        var mappings = CurrentMappings();

        if (atolItemType > 0)
        {
            var fromAtol = FindByAtolCode(mappings, atolItemType);
            if (fromAtol != null)
                return fromAtol.TrueApiGroupId;

            _logger.LogWarning("Нет маппинга Атол {AtolCode} → Честный знак", atolItemType);
        }

        if (string.IsNullOrWhiteSpace(gtin))
            return null;

        var catalog = await _gtinCatalogService.Get(gtin);
        if (catalog != null && catalog.TrueApiGroupId > 0)
            return catalog.TrueApiGroupId;

        return null;
    }

    /// <summary>
    /// Нужно ли проверять ЕМЦ (smp) для позиции.
    /// </summary>
    public bool ShouldCheckSmp(int atolItemType, int trueApiGroupId)
    {
        var mappings = CurrentMappings();

        if (atolItemType > 0)
        {
            var byAtol = FindByAtolCode(mappings, atolItemType);
            if (byAtol != null)
                return byAtol.CheckSmp;
        }

        if (trueApiGroupId > 0)
        {
            foreach (var item in mappings)
            {
                if (item.TrueApiGroupId == trueApiGroupId && item.CheckSmp)
                    return true;
            }

            return AtolToTrueApiGroupMap.DefaultCheckSmp(trueApiGroupId);
        }

        return false;
    }

    private List<GisMtProductMapping> CurrentMappings()
    {
        var mappings = _parametersService.Current().GisMtProductMappings;
        return mappings.Count > 0 ? mappings : AtolToTrueApiGroupMap.CopyDefaults();
    }

    private static GisMtProductMapping? FindByAtolCode(List<GisMtProductMapping> mappings, int atolCode)
    {
        foreach (var item in mappings)
        {
            if (item.AtolCode == atolCode)
                return item;
        }

        return null;
    }
}
