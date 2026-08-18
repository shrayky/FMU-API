using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.ProductGroups.Interfaces;
using FmuApiDomain.TrueApi.MarkData;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.ProductGroups;

/// <summary>
/// CRUD маппинга Атол → Честный знак в файле конфигурации.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtProductMappingService(
    IParametersService parametersService) : IGisMtProductMappingService
{
    private readonly IParametersService _parametersService = parametersService;

    public Task<List<GisMtProductMapping>> List()
    {
        var mappings = ResolveMappings(_parametersService.Current())
            .OrderBy(item => item.AtolCode)
            .ToList();

        return Task.FromResult(mappings);
    }

    public async Task<bool> Save(GisMtProductMapping mapping)
    {
        if (mapping.AtolCode <= 0 || mapping.TrueApiGroupId <= 0)
            return false;

        mapping.Name ??= string.Empty;

        var parameters = await _parametersService.CurrentAsync();
        EnsureSeeded(parameters);

        var existing = parameters.GisMtProductMappings
            .FirstOrDefault(item => item.AtolCode == mapping.AtolCode);

        if (existing == null)
        {
            parameters.GisMtProductMappings.Add(mapping);
        }
        else
        {
            existing.TrueApiGroupId = mapping.TrueApiGroupId;
            existing.Name = mapping.Name;
            existing.CheckSmp = mapping.CheckSmp;
        }

        await _parametersService.UpdateAsync(parameters);
        return true;
    }

    public async Task<bool> Delete(int atolCode)
    {
        if (atolCode <= 0)
            return false;

        var parameters = await _parametersService.CurrentAsync();
        EnsureSeeded(parameters);

        var removed = parameters.GisMtProductMappings.RemoveAll(item => item.AtolCode == atolCode);
        if (removed == 0)
            return false;

        await _parametersService.UpdateAsync(parameters);
        return true;
    }

    /// <summary>
    /// Если маппинг ещё не записан в конфиг — заполняет дефолтами.
    /// </summary>
    private static void EnsureSeeded(Parameters parameters)
    {
        if (parameters.GisMtProductMappings.Count == 0)
            parameters.GisMtProductMappings = AtolToTrueApiGroupMap.CopyDefaults();
    }

    /// <summary>
    /// Возвращает маппинг из конфига или дефолты, если список пуст.
    /// </summary>
    private static List<GisMtProductMapping> ResolveMappings(Parameters parameters)
        => parameters.GisMtProductMappings.Count > 0
            ? parameters.GisMtProductMappings
            : AtolToTrueApiGroupMap.CopyDefaults();
}
