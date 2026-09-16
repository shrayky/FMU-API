using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.State.Interfaces;

namespace FmuApiDomain.TrueApiIntegration;

/// <summary>
/// Применяет токены True API из пакета fmu-api-central и включает режим ГИС МТ с внешним токеном.
/// </summary>
public static class TrueApiTokensFromExchangeApplier
{
    public static bool Apply(
        IReadOnlyList<TrueApiTokenFromCentral>? tokens,
        IEnumerable<PrintGroupData> organisations,
        GisMtSettings gisMtSettings,
        IApplicationState applicationState)
    {
        if (tokens is null || tokens.Count == 0)
            return false;

        var orgsByInn = organisations
            .Where(organisation => !string.IsNullOrWhiteSpace(organisation.INN))
            .GroupBy(organisation => organisation.INN.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var settingsChanged = false;

        foreach (var item in tokens)
        {
            var inn = item.Inn?.Trim() ?? string.Empty;
            if (inn.Length == 0 || string.IsNullOrWhiteSpace(item.Token))
                continue;

            if (!orgsByInn.TryGetValue(inn, out var organisation))
                continue;

            var settings = organisation.TrueApiIntegrationSettings;

            if (!settings.Enable)
            {
                settings.Enable = true;
                settingsChanged = true;
            }

            if (!settings.UseExternalToken)
            {
                settings.UseExternalToken = true;
                settingsChanged = true;
            }

            if (!gisMtSettings.StockLoadEnabled)
            {
                gisMtSettings.StockLoadEnabled = true;
                settingsChanged = true;
            }

            applicationState.UpdateTrueApiToken(inn, item.Token, item.Expired);
        }

        return settingsChanged;
    }
}
