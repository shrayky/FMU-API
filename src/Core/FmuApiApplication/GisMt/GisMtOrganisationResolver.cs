using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options.Organization;

namespace FmuApiApplication.GisMt;

/// <summary>
/// Поиск организации в конфигурации по ИНН.
/// </summary>
internal static class GisMtOrganisationResolver
{
    /// <summary>
    /// Находит организацию по ИНН без учёта регистра и пробелов.
    /// </summary>
    public static PrintGroupData? Find(Parameters parameters, string inn)
    {
        var normalizedInn = inn.Trim();
        return parameters.OrganisationConfig.PrintGroups
            .FirstOrDefault(x => string.Equals(x.INN?.Trim(), normalizedInn, StringComparison.OrdinalIgnoreCase));
    }
}
