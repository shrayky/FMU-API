using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.DTO.FmuApiExchangeData.Answer;
using FmuApiDomain.DTO.FmuApiExchangeData.Request;
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
    
    public async Task<Result> DownloadAndApply(FmuApiCentralResponse response, string baseAddress, string token)
        {
            if (!response.SettingsUpdateAvailable)
                return Result.Success();

            _logger.LogInformation("В центральном сервере есть новые настройки для загрузки");

            var requestAddress = $"{baseAddress}/settings/{token}";
            var confirmAddress = $"{baseAddress}/settings/updated/{token}";
            
            return await DownloadSettingsData(requestAddress)
                .Bind(async rawData => await DecryptSettingsData(rawData).ConfigureAwait(false))
                .Bind(async settingsRaw => await DeserializeSettings(settingsRaw).ConfigureAwait(false))
                .Map(async loadedSettings => await ApplySettings(loadedSettings).ConfigureAwait(false))
                .Bind(async _ => await ConfirmDownload(baseAddress, confirmAddress).ConfigureAwait(false))
                .ConfigureAwait(false);
        }

        private async Task<Result<string>> DownloadSettingsData(string requestAddress)
            => await _exchangeService.DownloadNewConfiguration(requestAddress).ConfigureAwait(false);

        private async Task<Result<string>> DecryptSettingsData(string rawData)
        {
            var appSettings = await _parametersService.CurrentAsync().ConfigureAwait(false);
            
            var newSettingsRaw = !string.IsNullOrEmpty(appSettings.FmuApiCentralServer.Secret) ? 
                SecretString.DecryptData(rawData, appSettings.FmuApiCentralServer.Secret) : rawData;

            return Result.Success(newSettingsRaw);
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
        
        private async Task<Result> ConfirmDownload(string baseAddress, string confirmAddress)
            => await _exchangeService.ConfirmDownloadConfiguration(confirmAddress).ConfigureAwait(false);
}