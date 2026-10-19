using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.Configuration.Options.Organization;

namespace FmuApiDomain.TrueApiIntegration;

/// <summary>
/// Применяет x-api-key из пакета fmu-api-central: живой ключ записывает в организацию, истёкший помечает признаком.
/// </summary>
public static class XApiKeysFromExchangeApplier
{
    public static bool Apply(
        IReadOnlyList<XApiKeyFromCentral>? keys,
        IEnumerable<PrintGroupData> organisations)
    {
        if (keys is null || keys.Count == 0)
            return false;

        var groups = organisations.ToList();
        var changed = false;

        foreach (var item in keys)
        {
            var inn = (item.Inn ?? string.Empty).Trim();
            if (inn.Length == 0)
                continue;

            var matches = groups
                .Where(group => string.Equals((group.INN ?? string.Empty).Trim(), inn, StringComparison.Ordinal))
                .ToList();

            if (matches.Count == 0)
                continue;

            if (item.XApiKeyExpired)
            {
                foreach (var group in matches)
                {
                    if (group.XApiKeyExpired)
                        continue;

                    group.XApiKeyExpired = true;
                    changed = true;
                }

                continue;
            }

            var key = (item.XApiKey ?? string.Empty).Trim();
            if (key.Length == 0)
                continue;

            foreach (var group in matches)
            {
                if (group.XAPIKEY == key && !group.XApiKeyExpired)
                    continue;

                group.XAPIKEY = key;
                group.XApiKeyExpired = false;
                changed = true;
            }
        }

        return changed;
    }
}
