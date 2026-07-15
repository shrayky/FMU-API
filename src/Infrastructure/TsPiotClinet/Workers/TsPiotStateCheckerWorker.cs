using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Json;
using System.Globalization;
using TsPiotClinet.Models;
using TsPiotClinet.Services;

namespace TsPiotClinet.Workers
{
    public class TsPiotStateCheckerWorker(
        ILogger<TsPiotStateCheckerWorker> logger,
        IParametersService parametersService,
        IApplicationState applicationState,
        IHttpClientFactory httpClientFactory,
        TsPiotEspApiService tsPiotEspApiService) : BackgroundService
    {
        private readonly ILogger<TsPiotStateCheckerWorker> _logger = logger;
        private readonly IParametersService _parametersService = parametersService;
        private readonly IApplicationState _applicationState = applicationState;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly TsPiotEspApiService _tsPiotEspApiService = tsPiotEspApiService;

        private const int StartDelayInSeconds = 10;
        private const int CheckIntervalInMinutes = 10;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            await Task.Delay(TimeSpan.FromSeconds(StartDelayInSeconds), stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                var appSettings = await _parametersService.CurrentAsync();

                if (appSettings.ServerConfig.TsPiotEnabled)
                {
                    await CheckTsPiotState(appSettings.OrganisationConfig.PrintGroups);
                }

                await Task.Delay(TimeSpan.FromMinutes(CheckIntervalInMinutes), stoppingToken).ConfigureAwait(false);
            }
        }

        private async Task CheckTsPiotState(List<PrintGroupData> printGroups)
        {
            foreach (var printGroup in printGroups)
            {
                if (string.IsNullOrEmpty(printGroup.TsPiot.Host) || string.IsNullOrEmpty(printGroup.TsPiot.Port))
                    continue;

                var address = $"{printGroup.TsPiot.Host}:{printGroup.TsPiot.Port}";

                var checkModuleVersion = await AskModuleVersion(printGroup.TsPiot);

                var version = checkModuleVersion.IsSuccess ? checkModuleVersion.Value : "-";

                var checkProtocolResult = await AskProtocolVersion(printGroup.TsPiot);
                var protocol = 1;

                if (checkProtocolResult.IsSuccess)
                    protocol = checkProtocolResult.Value;

                _applicationState.TsPiotApiVersion(address, protocol, version);

                if (checkProtocolResult.IsFailure)
                    _applicationState.TsPiotOffline(address);

                var instancesResult = await _tsPiotEspApiService.Instances(printGroup.TsPiot);
                if (instancesResult.IsFailure)
                    continue;

                await SyncLicenses(printGroups, printGroup.TsPiot, instancesResult.Value.Instances);
                await SyncInstanceSettings(printGroup.TsPiot, instancesResult.Value.Instances);
            }
        }

        private async Task<Result<string>> AskModuleVersion(TsPiotConnectionSettings tsPiot)
        {
            var moduleInfoResult = await _tsPiotEspApiService.ModuleInfo(tsPiot);

            if (moduleInfoResult.IsFailure)
                return Result.Failure<string>("-");

            _logger.LogInformation("Используется ТСПиОТ версии {Version}", moduleInfoResult.Value.Version);
            return Result.Success(moduleInfoResult.Value.Version);
        }

        private async Task<Result<int>> AskProtocolVersion(TsPiotConnectionSettings tsPiot)
        {
            using var httpClient = _httpClientFactory.CreateClient("TsPiotStateChecker");

            var addressPrefix = "https://";

            if (tsPiot.Host.Contains("https://"))
                addressPrefix = "";

            var url = $"{addressPrefix}{tsPiot.Host}:{tsPiot.Port}";
            httpClient.BaseAddress = new Uri(url);

            for (var protocolVersion = 3; protocolVersion > 0; protocolVersion--)
            {
                var requestPath = $"/api/v{protocolVersion}/info";

                try
                {
                    var response = await httpClient.GetAsync(requestPath);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();

                        _logger.LogDebug("Ошибка в ответе проверки версии протокола от ТСПИоТ: {StatusCode}, {Error}", response.StatusCode,
                            errorContent);

                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Ответ ТСПИоТ версии протокола: {content}", content);

                    var status = await JsonHelpers.DeserializeAsync<TsPiotKktInfo>(content);

                    if (status != null)
                    {
                        _logger.LogInformation("Используется ТСПиОТ с {v} версией протокола.", protocolVersion);
                        return Result.Success(protocolVersion);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Ошибка проверки версии протокола от ТСПИоТ: {ex}", ex);
                    continue;
                }
            }

            return Result.Failure<int>($"Не удалось подключится к экземпляру ТСПиОТ {tsPiot.Host}:{tsPiot.Port}");
        }

        private async Task SyncLicenses(List<PrintGroupData> printGroups, TsPiotConnectionSettings tsPiot, List<TsPiotInstanceListItem> instances)
        {
            foreach (var instance in instances)
            {
                if (string.IsNullOrEmpty(instance.Id))
                    continue;

                var instanceDetailResult = await _tsPiotEspApiService.InstanceDetail(tsPiot, instance.Id);
                if (instanceDetailResult.IsFailure)
                    continue;

                var instanceDetail = instanceDetailResult.Value;
                var kktInn = instanceDetail.RegData.KktInn;

                if (string.IsNullOrEmpty(kktInn))
                    continue;

                var activeLicense = instanceDetail.Licenses.FirstOrDefault(l => l.IsActive);

                if (activeLicense == null || string.IsNullOrEmpty(activeLicense.ActiveTill))
                    continue;

                if (!DateTime.TryParse(activeLicense.ActiveTill, CultureInfo.InvariantCulture, DateTimeStyles.None, out var licenseActiveTill))
                    continue;

                var organization = printGroups.FirstOrDefault(p => p.INN == kktInn);

                if (organization == null || string.IsNullOrEmpty(organization.TsPiot.Host) || string.IsNullOrEmpty(organization.TsPiot.Port))
                    continue;

                var address = $"{organization.TsPiot.Host}:{organization.TsPiot.Port}";
                _applicationState.UpdateTsPiotLicense(address, organization.Id, licenseActiveTill);
            }
        }

        private async Task SyncInstanceSettings(TsPiotConnectionSettings tsPiot, List<TsPiotInstanceListItem> instances)
        {
            var appSettings = await _parametersService.CurrentAsync();

            foreach (var instance in instances)
            {
                if (string.IsNullOrEmpty(instance.Id))
                    continue;

                var settingsResult = await _tsPiotEspApiService.InstanceSettings(tsPiot, instance.Id);
                if (settingsResult.IsFailure)
                    continue;

                var settings = settingsResult.Value;
                var needUpdateSettings = false;

                if (settings.CdnCodesCheckTimeout != appSettings.HttpRequestTimeouts.CheckMarkRequestTimeout * 1000 
                    && appSettings.HttpRequestTimeouts.SyncWithTsPiot)
                {
                    settings.CdnCodesCheckTimeout = appSettings.HttpRequestTimeouts.CheckMarkRequestTimeout * 1000;
                    needUpdateSettings = true;
                }

                if (settings.CdnHealthCheckTimeout != appSettings.HttpRequestTimeouts.CdnRequestTimeout * 1000
                    && appSettings.HttpRequestTimeouts.SyncWithTsPiot)
                {
                    settings.CdnHealthCheckTimeout = appSettings.HttpRequestTimeouts.CdnRequestTimeout * 1000;
                    needUpdateSettings = true;
                }

                if (!settings.AllowRemoteConnection)
                {
                    settings.AllowRemoteConnection = true;
                    needUpdateSettings = true;
                }

                if (!needUpdateSettings)
                    continue;

                await _tsPiotEspApiService.UpdateInstanceSettings(tsPiot, instance.Id, settings);
            }
        }
    }
}
