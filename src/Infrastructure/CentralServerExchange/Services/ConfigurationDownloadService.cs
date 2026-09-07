using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.CentralServiceExchange.Models;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Json;
using Shared.Strings;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class ConfigurationDownloadService
{
    private readonly ILogger<ConfigurationDownloadService> _logger;
    private readonly IParametersService _parametersService;
    private readonly IExchangeService _exchangeService;

    public ConfigurationDownloadService(ILogger<ConfigurationDownloadService> logger, IParametersService parametersService, IExchangeService exchangeService)
    {
        _logger = logger;
        _parametersService = parametersService;
        _exchangeService = exchangeService;
    }

    public async Task<Result> DownloadAndApply(FmuApiCentralResponse response, string baseAddress, string token, string? bearerToken = null)
    {
        if (!response.SettingsUpdateAvailable)
            return Result.Success();

        _logger.LogInformation("В центральном сервере есть новые настройки для загрузки");

        var requestAddress = string.IsNullOrEmpty(bearerToken)
            ? $"{baseAddress}/settings/{token}"
            : $"{baseAddress}/settings";
        var confirmAddress = string.IsNullOrEmpty(bearerToken)
            ? $"{baseAddress}/settings/updated/{token}"
            : $"{baseAddress}/settings/updated";

        return await DownloadSettingsData(requestAddress, bearerToken)
            .Map(JsonStringPayload.Unwrap)
            .Bind(async rawData => await DecryptSettingsData(rawData).ConfigureAwait(false))
            .Bind(async settingsRaw => await DeserializeSettings(settingsRaw).ConfigureAwait(false))
            .Bind(async loadedSettings => await ApplySettings(loadedSettings).ConfigureAwait(false))
            .Bind(async () => await ConfirmDownload(confirmAddress, bearerToken).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    private async Task<Result<string>> DownloadSettingsData(string requestAddress, string? bearerToken)
        => await _exchangeService.DownloadNewConfiguration(requestAddress, bearerToken).ConfigureAwait(false);

    private async Task<Result<string>> DecryptSettingsData(string rawData)
    {
        try
        {
            var appSettings = await _parametersService.CurrentAsync().ConfigureAwait(false);

            var newSettingsRaw = !string.IsNullOrEmpty(appSettings.FmuApiCentralServer.Secret)
                ? SecretString.DecryptData(rawData, appSettings.FmuApiCentralServer.Secret)
                : rawData;

            return Result.Success(newSettingsRaw);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Ошибка расшифровки настроек: {ex.Message}");
        }
    }

    private async Task<Result<FmuApiSetting>> DeserializeSettings(string settingsRaw)
    {
        try
        {
            var newSettings = await JsonHelpers.DeserializeAsync<FmuApiSetting>(settingsRaw).ConfigureAwait(false);

            return newSettings != null
                ? Result.Success(newSettings)
                : Result.Failure<FmuApiSetting>("Не удалось десериализовать пакет настроек от центрального сервера");
        }
        catch (Exception ex)
        {
            return Result.Failure<FmuApiSetting>($"Ошибка десериализации: {ex.Message}");
        }
    }

    private async Task<Result> ApplySettings(FmuApiSetting newSettings)
        => await _parametersService.ApplyFromCentral(newSettings).ConfigureAwait(false);

    private async Task<Result> ConfirmDownload(string confirmAddress, string? bearerToken)
        => await _exchangeService.ConfirmDownloadConfiguration(confirmAddress, bearerToken).ConfigureAwait(false);
}
