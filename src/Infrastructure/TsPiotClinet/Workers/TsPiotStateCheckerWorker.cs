using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Documents;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TsPiot.Interfaces;
using FmuApiDomain.TsPiot.Models;
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
        TsPiotEspApiService tsPiotEspApiService,
        IPiotSettingsService piotSettingsService) : BackgroundService
    {
        private readonly ILogger<TsPiotStateCheckerWorker> _logger = logger;
        private readonly IParametersService _parametersService = parametersService;
        private readonly IApplicationState _applicationState = applicationState;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly TsPiotEspApiService _tsPiotEspApiService = tsPiotEspApiService;
        private readonly IPiotSettingsService _piotSettingsService = piotSettingsService;

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
                    await PushDeviceSettings(appSettings.HttpRequestTimeouts, stoppingToken);
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

                var address = Address(printGroup);

                var checkModuleVersion = await AskModuleVersion(printGroup.TsPiot);

                var version = checkModuleVersion.IsSuccess ? checkModuleVersion.Value : "-";

                var checkProtocolResult = await AskProtocolVersion(printGroup.TsPiot);
                var protocol = 1;
                var lastStatusCode = checkProtocolResult.StatusCode;

                if (checkProtocolResult.Protocol.IsSuccess)
                    protocol = checkProtocolResult.Protocol.Value;

                ApplyTsPiotInfoAnswer(printGroup, checkProtocolResult.IsAnswered, checkProtocolResult.KktInfo);

                _applicationState.TsPiotApiVersion(address, protocol, version);

                if (checkProtocolResult.Protocol.IsFailure)
                    _applicationState.TsPiotOffline(address);

                _applicationState.UpdateTsPiotLastCheckStatusCode(address, lastStatusCode);

                var instancesResult = await _tsPiotEspApiService.Instances(printGroup.TsPiot);
                if (instancesResult.IsFailure)
                    continue;

                await SyncLicenses(printGroups, printGroup.TsPiot, instancesResult.Value.Instances);
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

        /// <summary>
        /// Результат опроса <c>/api/v{1..3}/info</c>.
        /// <c>IsAnswered</c> — получено и разобрано тело ответа: только в этом случае можно менять сохранённый токен.
        /// </summary>
        internal record TsPiotInfoAnswer(Result<int> Protocol, int StatusCode, TsPiotKktInfo? KktInfo, bool IsAnswered);

        /// <summary>
        /// Определяет версию протокола ТС ПИоТ и фиксирует HTTP-код последнего ответа.
        /// </summary>
        internal async Task<TsPiotInfoAnswer> AskProtocolVersion(TsPiotConnectionSettings tsPiot)
        {
            using var httpClient = _httpClientFactory.CreateClient("TsPiotStateChecker");

            var addressPrefix = "https://";

            if (tsPiot.Host.Contains("https://"))
                addressPrefix = "";

            var url = $"{addressPrefix}{tsPiot.Host}:{tsPiot.Port}";
            httpClient.BaseAddress = new Uri(url);

            var lastStatusCode = 0;

            for (var protocolVersion = 3; protocolVersion > 0; protocolVersion--)
            {
                var requestPath = $"/api/v{protocolVersion}/info";

                try
                {
                    var response = await httpClient.GetAsync(requestPath);
                    lastStatusCode = (int)response.StatusCode;

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();

                        _logger.LogDebug("Ошибка в ответе проверки версии протокола от ТСПИоТ: {StatusCode}, {Error}", response.StatusCode,
                            errorContent);

                        if (lastStatusCode >= 500)
                            break;

                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();

                    _logger.LogDebug("Ответ ТСПИоТ версии протокола: {content}", content);

                    var status = await JsonHelpers.DeserializeAsync<TsPiotKktInfo>(content);

                    if (status != null)
                    {
                        _logger.LogInformation("Используется ТСПиОТ с {v} версией протокола.", protocolVersion);
                        return new TsPiotInfoAnswer(Result.Success(protocolVersion), lastStatusCode, status, true);
                    }

                    // Тело ответа разобрано, но модель не получена: токен менять нельзя, пробуем следующую версию протокола.
                    _logger.LogWarning("Ответ info ТСПИоТ версии протокола {Version} не разобран", protocolVersion);
                }
                catch (Exception ex)
                {
                    lastStatusCode = 0;
                    _logger.LogDebug("Ошибка проверки версии протокола от ТСПИоТ: {ex}", ex);
                    continue;
                }
            }

            return new TsPiotInfoAnswer(
                Result.Failure<int>($"Не удалось подключится к экземпляру ТСПиоТ {tsPiot.Host}:{tsPiot.Port}"),
                lastStatusCode,
                null,
                false);
        }

        private static string Address(PrintGroupData printGroup)
        {
            return $"{printGroup.TsPiot.Host}:{printGroup.TsPiot.Port}";
        }

        /// <summary>
        /// Применяет результат опроса info к сохранённому токену ТС ПИоТ организации.
        /// </summary>
        internal void ApplyTsPiotInfoAnswer(PrintGroupData printGroup, bool isAnswered, TsPiotKktInfo? kktInfo)
        {
            // Токен меняем только тогда, когда info ответил и тело разобрано:
            // таймаут, 5xx или неразобранный ответ не должны снимать запрет на инициализацию ЛМ.
            if (!isAnswered)
            {
                _logger.LogDebug("Ответ info ТСПиоТ для организации {OrganizationId} не получен, сохранённый токен не изменяется", printGroup.Id);
                return;
            }

            SaveTsPiotCredential(printGroup, kktInfo);
        }

        /// <summary>
        /// Сохраняет токен, которым ТС ПИоТ инициализировал ЛМ организации.
        /// Вызывается только для разобранного ответа info: пустой <c>lm.token</c> очищает сохранённое значение.
        /// </summary>
        private void SaveTsPiotCredential(PrintGroupData printGroup, TsPiotKktInfo? kktInfo)
        {
            var localModule = kktInfo?.LocalModeStatus;
            var token = localModule?.Token;

            if (string.IsNullOrWhiteSpace(token))
            {
                _applicationState.UpdateLocalModuleTsPiotCredential(printGroup.Id, string.Empty, null, string.Empty);
                return;
            }

            var expiresAtUtc = ParseTokenExpirationDate(printGroup, localModule!.TokenExpirationDate);

            _applicationState.UpdateLocalModuleTsPiotCredential(
                printGroup.Id,
                token,
                expiresAtUtc,
                kktInfo!.FnSerialNumber);

            _logger.LogInformation("Получен токен ТС ПИоТ для организации {OrganizationId}", printGroup.Id);
        }

        /// <summary>
        /// Разбирает дату истечения токена (ISO 8601, UTC). Неразобранную дату считает отсутствующей:
        /// непустой токен в этом случае считается действующим.
        /// </summary>
        private DateTime? ParseTokenExpirationDate(PrintGroupData printGroup, string expDate)
        {
            if (string.IsNullOrWhiteSpace(expDate))
                return null;

            if (DateTime.TryParse(expDate, CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
                return parsed;

            _logger.LogWarning(
                "Не удалось разобрать дату истечения токена ТС ПИоТ {ExpDate} для организации {OrganizationId}, токен считается действующим",
                expDate,
                printGroup.Id);

            return null;
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

                var address = Address(organization);
                _applicationState.UpdateTsPiotLicense(address, organization.Id, licenseActiveTill);
            }
        }

        private async Task PushDeviceSettings(HttpRequestTimeouts timeouts, CancellationToken cancellationToken)
        {
            var deviceSettings = new PiotDeviceSettings
            {
                AllowRemoteConnection = true,
                CdnCodesCheckTimeoutMs = timeouts.SyncWithTsPiot ? timeouts.CheckMarkRequestTimeout * 1000 : null,
                CdnHealthCheckTimeoutMs = timeouts.SyncWithTsPiot ? timeouts.CdnRequestTimeout * 1000 : null
            };

            var pushResult = await _piotSettingsService.PushSettings(deviceSettings, cancellationToken);
            if (pushResult.IsFailure)
                _logger.LogWarning("Не удалось отправить настройки в ТС ПИоТ: {Error}", pushResult.Error);
        }
    }
}
