using FmuApiApplication.TrueApi.Models;
using FmuApiDomain.Authentication.Models;
using FmuApiDomain.Configuration;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.TrueApi;

/// <summary>
/// Собирает статусы токенов True API по организациям с включённой интеграцией.
/// </summary>
public static class TrueApiTokenStateCollector
{
    public static List<TrueApiTokenStateInformation> Collect(Parameters settings, IApplicationState appState)
    {
        List<TrueApiTokenStateInformation> states = [];

        foreach (var organisation in settings.OrganisationConfig.PrintGroups)
        {
            if (!organisation.TrueApiIntegrationSettings.Enable)
                continue;

            var token = appState.TrueApiToken(organisation.INN.Trim());
            var loaded = !string.IsNullOrEmpty(token.Token);
            var expired = loaded ? token.ExpirationDate() : (DateTime?)null;

            states.Add(new TrueApiTokenStateInformation
            {
                Id = organisation.Id,
                Organization = organisation.Id,
                Name = organisation.Name,
                Inn = organisation.INN.Trim(),
                Loaded = loaded,
                Expired = expired,
                Status = FormatStatus(token)
            });
        }

        return states;
    }

    public static string FormatStatus(TokenData token)
    {
        if (string.IsNullOrEmpty(token.Token))
            return "Не загружен";

        return $"действует до {token.ExpirationDate():dd.MM.yyyy}";
    }
}
