using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using Microsoft.Extensions.Logging;
using Shared.Json;
using System.Text;
using TsPiotClinet.Models;

namespace TsPiotClinet.Services;

public class TsPiotEspApiService(ILogger<TsPiotEspApiService> logger, IHttpClientFactory httpClientFactory)
{
    private readonly ILogger<TsPiotEspApiService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<Result<TsPiotModuleInfo>> ModuleInfo(TsPiotConnectionSettings settings)
    {
        if (string.IsNullOrEmpty(settings.InformationEndpoint) || settings.InformationPort <= 0)
            return Result.Failure<TsPiotModuleInfo>("Не задан information endpoint или порт");

        var baseAddress = BuildInformationBaseAddress(settings);
        if (baseAddress == null)
            return Result.Failure<TsPiotModuleInfo>("Некорректный адрес information API");

        using var httpClient = CreateHttpClient(baseAddress);

        try
        {
            var response = await httpClient.GetAsync(settings.InformationEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Ошибка получения версии модуля ТС ПиОТ: {StatusCode}, {Error}", response.StatusCode, errorContent);
                return Result.Failure<TsPiotModuleInfo>("Ошибка ответа information API");
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Ответ ТС ПиОТ версии модуля: {Content}", content);

            var moduleInfo = await JsonHelpers.DeserializeAsync<TsPiotModuleInfo>(content);
            if (moduleInfo == null)
                return Result.Failure<TsPiotModuleInfo>("Пустой ответ information API");

            return Result.Success(moduleInfo);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Ошибка запроса версии модуля ТС ПиОТ: {Exception}", ex);
            return Result.Failure<TsPiotModuleInfo>("Ошибка запроса information API");
        }
    }

    public async Task<Result<TsPiotInstancesInfoResponse>> Instances(TsPiotConnectionSettings settings)
    {
        if (settings.InformationPort <= 0)
            return Result.Failure<TsPiotInstancesInfoResponse>("Не задан information порт");

        var baseAddress = BuildInformationBaseAddress(settings);
        if (baseAddress == null)
            return Result.Failure<TsPiotInstancesInfoResponse>("Некорректный адрес information API");

        using var httpClient = CreateHttpClient(baseAddress);

        try
        {
            var response = await httpClient.GetAsync("/api/v1/instances/info");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Ошибка получения списка инстансов ТС ПиОТ: {StatusCode}, {Error}", response.StatusCode, errorContent);
                return Result.Failure<TsPiotInstancesInfoResponse>("Ошибка ответа information API");
            }

            var content = await response.Content.ReadAsStringAsync();
            var instancesInfo = await JsonHelpers.DeserializeAsync<TsPiotInstancesInfoResponse>(content);

            if (instancesInfo?.Instances == null)
                return Result.Failure<TsPiotInstancesInfoResponse>("Пустой ответ information API");

            return Result.Success(instancesInfo);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Ошибка запроса списка инстансов ТС ПиОТ: {Exception}", ex);
            return Result.Failure<TsPiotInstancesInfoResponse>("Ошибка запроса information API");
        }
    }

    public async Task<Result<TsPiotInstanceDetailResponse>> InstanceDetail(TsPiotConnectionSettings settings, string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return Result.Failure<TsPiotInstanceDetailResponse>("Не задан идентификатор инстанса");

        var baseAddress = BuildInformationBaseAddress(settings);
        if (baseAddress == null)
            return Result.Failure<TsPiotInstanceDetailResponse>("Некорректный адрес information API");

        using var httpClient = CreateHttpClient(baseAddress);

        try
        {
            var response = await httpClient.GetAsync($"/api/v1/instances/info/{instanceId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Ошибка получения информации об инстансе {InstanceId}: {StatusCode}, {Error}", instanceId, response.StatusCode, errorContent);
                return Result.Failure<TsPiotInstanceDetailResponse>("Ошибка ответа information API");
            }

            var content = await response.Content.ReadAsStringAsync();
            var instanceDetail = await JsonHelpers.DeserializeAsync<TsPiotInstanceDetailResponse>(content);

            if (instanceDetail == null)
                return Result.Failure<TsPiotInstanceDetailResponse>("Пустой ответ information API");

            return Result.Success(instanceDetail);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Ошибка запроса информации об инстансе {InstanceId}: {Exception}", instanceId, ex);
            return Result.Failure<TsPiotInstanceDetailResponse>("Ошибка запроса information API");
        }
    }

    public async Task<Result<TsPiotModuleSettings>> InstanceSettings(TsPiotConnectionSettings settings, string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return Result.Failure<TsPiotModuleSettings>("Не задан идентификатор инстанса");

        var baseAddress = BuildInformationBaseAddress(settings);
        if (baseAddress == null)
            return Result.Failure<TsPiotModuleSettings>("Некорректный адрес information API");

        using var httpClient = CreateHttpClient(baseAddress);

        try
        {
            var response = await httpClient.GetAsync($"/api/v1/settings/{instanceId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Ошибка получения настроек инстанса {InstanceId}: {StatusCode}, {Error}", instanceId, response.StatusCode, errorContent);
                return Result.Failure<TsPiotModuleSettings>("Ошибка ответа information API");
            }

            var content = await response.Content.ReadAsStringAsync();
            var moduleSettings = await JsonHelpers.DeserializeAsync<TsPiotModuleSettings>(content);

            if (moduleSettings == null)
                return Result.Failure<TsPiotModuleSettings>("Пустой ответ information API");

            return Result.Success(moduleSettings);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Ошибка запроса настроек инстанса {InstanceId}: {Exception}", instanceId, ex);
            return Result.Failure<TsPiotModuleSettings>("Ошибка запроса information API");
        }
    }

    public async Task<Result> UpdateInstanceSettings(TsPiotConnectionSettings settings, string instanceId, TsPiotModuleSettings moduleSettings)
    {
        if (string.IsNullOrEmpty(instanceId))
            return Result.Failure("Не задан идентификатор инстанса");

        var baseAddress = BuildInformationBaseAddress(settings);
        if (baseAddress == null)
            return Result.Failure("Некорректный адрес information API");

        using var httpClient = CreateHttpClient(baseAddress);

        try
        {
            var json = await JsonHelpers.SerializeAsync(moduleSettings);
            var response = await httpClient.PutAsync(
                $"/api/v1/settings/{instanceId}",
                new StringContent(json, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Ошибка обновления настроек инстанса {InstanceId}: {StatusCode}, {Error}", instanceId, response.StatusCode, errorContent);
                return Result.Failure("Ошибка ответа information API");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Ошибка обновления настроек инстанса {InstanceId}: {Exception}", instanceId, ex);
            return Result.Failure("Ошибка запроса information API");
        }
    }

    private HttpClient CreateHttpClient(Uri baseAddress)
    {
        var httpClient = _httpClientFactory.CreateClient("TsPiotVerisonChecker");
        httpClient.BaseAddress = baseAddress;
        return httpClient;
    }

    private static Uri? BuildInformationBaseAddress(TsPiotConnectionSettings settings)
    {
        var address = $"{settings.Host}:{settings.InformationPort}";

        if (address.Contains("https://"))
            address = address.Replace("https://", "http://");

        if (!address.Contains("http://"))
            address = $"http://{address}";

        return Uri.TryCreate(address, UriKind.Absolute, out var uri) ? uri : null;
    }
}
