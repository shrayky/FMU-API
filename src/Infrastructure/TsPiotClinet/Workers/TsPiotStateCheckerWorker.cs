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

namespace TsPiotClinet.Workers
{
    public class TsPiotStateCheckerWorker : BackgroundService
    {
        private readonly ILogger<TsPiotStateCheckerWorker> _logger;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _applicationState;
        private readonly IHttpClientFactory _httpClientFactory;

        private const int StartDelayInSeconds = 10;
        private const int CheckIntervalInMinutes = 10;

        public TsPiotStateCheckerWorker(ILogger<TsPiotStateCheckerWorker> logger, IParametersService parametersService, IApplicationState applicationState, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _parametersService = parametersService;
            _applicationState = applicationState;
            _httpClientFactory = httpClientFactory;
        }

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

                await FetchLicenseInfo(printGroups, printGroup.TsPiot);
            }
        }

        private async Task<Result<string>> AskModuleVersion(TsPiotConnectionSettings tsPiot)
        {
            if (string.IsNullOrEmpty(tsPiot.InformationEndpoint) || tsPiot.InformationPort <= 0)
                return Result.Failure<string>("-");

            var address = $"{tsPiot.Host}:{tsPiot.InformationPort}";

            if (address.Contains("https://"))
            {
                address = address.Replace("https://", "http://");
            }

            if (!address.Contains("http://"))
            {
                address = $"http://{address}";
            }

            using var httpClient = _httpClientFactory.CreateClient("TsPiotVerisonChecker");
            var url = address;
            httpClient.BaseAddress = new Uri(url);

            try
            {
                var response = await httpClient.GetAsync(tsPiot.InformationEndpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Ошибка в ответе проверки версии модуля ТСПИоТ: {StatusCode}, {Error}", response.StatusCode,
                        errorContent);

                    return Result.Failure<string>("-");
                }

                var content = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("Ответ ТСПИоТ версии модуля: {content}", content);

                var status = await JsonHelpers.DeserializeAsync<TsPiotModuleInfo>(content);

                if (status != null)
                {
                    _logger.LogInformation("Используется ТСПиОТ версии {v}", status.Version);
                    return Result.Success(status.Version);
                }

            }
            catch (Exception ex)
            {
                _logger.LogDebug("Ошибка проверки версии модуля ТСПИоТ: {ex}", ex);
                return Result.Failure<string>("-");
            }

            return Result.Failure<string>("-");
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

        private async Task FetchLicenseInfo(List<PrintGroupData> printGroups, TsPiotConnectionSettings tsPiot)
        {
            if (tsPiot.InformationPort <= 0)
                return;

            var baseAddress = BuildInformationBaseAddress(tsPiot);

            if (baseAddress == null)
                return;

            using var httpClient = _httpClientFactory.CreateClient("TsPiotVerisonChecker");
            httpClient.BaseAddress = baseAddress;

            try
            {
                var listResponse = await httpClient.GetAsync("/api/v1/instances/info");
                if (!listResponse.IsSuccessStatusCode)
                    return;

                var listContent = await listResponse.Content.ReadAsStringAsync();
                var instancesInfo = await JsonHelpers.DeserializeAsync<TsPiotInstancesInfoResponse>(listContent);
                if (instancesInfo?.Instances == null)
                    return;

                foreach (var instance in instancesInfo.Instances)
                {
                    if (string.IsNullOrEmpty(instance.Id))
                        continue;

                    var detailResponse = await httpClient.GetAsync($"/api/v1/instances/info/{instance.Id}");

                    if (!detailResponse.IsSuccessStatusCode)
                        continue;

                    var detailContent = await detailResponse.Content.ReadAsStringAsync();

                    var instanceDetail = await JsonHelpers.DeserializeAsync<TsPiotInstanceDetailResponse>(detailContent);
                    if (instanceDetail == null)
                        continue;

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
            catch (Exception ex)
            {
                _logger.LogDebug("Ошибка получения срока лицензии ТСПИоТ: {ex}", ex);
            }
        }
        
        private static Uri? BuildInformationBaseAddress(TsPiotConnectionSettings tsPiot)
        {
            var address = $"{tsPiot.Host}:{tsPiot.InformationPort}";

            if (address.Contains("https://"))
                address = address.Replace("https://", "http://");

            if (!address.Contains("http://"))
                address = $"http://{address}";

            return Uri.TryCreate(address, UriKind.Absolute, out var uri) ? uri : null;
        }
    }
}
