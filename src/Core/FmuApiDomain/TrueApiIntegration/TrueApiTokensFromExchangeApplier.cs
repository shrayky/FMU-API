using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.State.Interfaces;

namespace FmuApiDomain.TrueApiIntegration;

/// <summary>
/// Применяет токены True API из ответа обмена с fmu-api-central по ИНН организаций.
/// </summary>
public static class TrueApiTokensFromExchangeApplier
{
    public static void Apply(
        IReadOnlyList<TrueApiTokenFromCentral>? tokens,
        IEnumerable<PrintGroupData> organisations,
        IApplicationState applicationState)
    {
        if (tokens is null || tokens.Count == 0)
            return;

        var orgsByInn = organisations
            .Where(organisation => !string.IsNullOrWhiteSpace(organisation.INN))
            .GroupBy(organisation => organisation.INN.Trim(), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var item in tokens)
        {
            var inn = item.Inn?.Trim() ?? string.Empty;
            if (inn.Length == 0 || string.IsNullOrWhiteSpace(item.Token))
                continue;

            if (!orgsByInn.TryGetValue(inn, out var organisation))
                continue;

            var settings = organisation.TrueApiIntegrationSettings;
            if (!settings.Enable || !settings.UseExternalToken)
                continue;

            applicationState.UpdateTrueApiToken(inn, item.Token, item.Expired);
        }
    }
}
