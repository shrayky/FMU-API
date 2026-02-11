using FmuApiDomain.Configuration.Options;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.TrueApi.MarkData.Check;
using LocalModuleIntegration.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LocalModuleIntegration.Service;

public class LocalModuleServiceV2 : ILocalModuleService
{
    private readonly ILogger<LocalModuleServiceV1> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private const string InitAddress = @"/api/v2/init";
    private const string StatusAddress = @"/api/v2/status";
    private const string OutCheckAddress = @"/api/v2/cis/outCheck";

    public LocalModuleServiceV2(IHttpClientFactory httpClientFactory, ILogger<LocalModuleServiceV1> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> InitializeAsync(LocalModuleConnection connection, string token)
    {
        if (!connection.Enable)
        {
            _logger.LogWarning("Попытка инициализации отключенного ЛМ");
            return false;
        }

        using var httpClient = _httpClientFactory.CreateClient("LocalModule");

        httpClient.BaseAddress = new Uri(connection.ConnectionAddress);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", connection.GetBasicAuthorizationHeader());

        var content = new
        {
            token = token
        };

        var response = await httpClient.PostAsJsonAsync(InitAddress, content);

        if (response.IsSuccessStatusCode)
            return response.IsSuccessStatusCode;
            
        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogError(
            "Ошибка при инициализации ЛМ. Код: {StatusCode}, Причина: {ReasonPhrase}, Тело: {ErrorContent}",
            (int)response.StatusCode,
            response.ReasonPhrase,
            errorContent
        );

        return response.IsSuccessStatusCode;
    }
        
    public async Task<LocalModuleState> StateAsync(LocalModuleConnection connection)
    {
        if (!connection.Enable)
            return new LocalModuleState();

        if (!await EniseyOnline(connection.EniseyConnectionAddress))
        {
            return new LocalModuleState()
            {
                StatusRaw = "enisey_off-line"
            };
        }

        using var httpClient = _httpClientFactory.CreateClient("LocalModule");
        httpClient.BaseAddress = new Uri(connection.ConnectionAddress);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", connection.GetBasicAuthorizationHeader());

        var response = await httpClient.GetAsync(StatusAddress);

        var state = await JsonHelpers.DeserializeAsync<LocalModuleState>(await response.Content.ReadAsStreamAsync());

        if (response.IsSuccessStatusCode) 
            return state ?? new LocalModuleState();
            
        var errorContent = await response.Content.ReadAsStringAsync();
            
        _logger.LogError(
            "Ошибка получения статуса ЛМ. Код: {StatusCode}, Причина: {ReasonPhrase}, Тело: {ErrorContent}",
            (int)response.StatusCode,
            response.ReasonPhrase,
            errorContent
        );
                
        return new LocalModuleState();
    }

    public async Task<CheckMarksDataTrueApi> OutCheckAsync(LocalModuleConnection connection, string cis, string xapiKey)
    {
        using var httpClient = _httpClientFactory.CreateClient("LocalModule");

        httpClient.BaseAddress = new Uri(connection.ConnectionAddress);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", connection.GetBasicAuthorizationHeader());

        var encodedCis = Uri.EscapeDataString(cis);

        var cises = new[]
        {
            new
            {
                cis = encodedCis
            }
        };

        var content = new
        {
            cis_list = cises
        };
            
        var response = await httpClient.PostAsJsonAsync(OutCheckAddress, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogWarning("Ошибка в ответе от локального модуля: {err}", errorContent);
            
            return new CheckMarksDataTrueApi();
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        
        CheckMarksDataTrueApi? status = null;
        
        var wrapper = await JsonHelpers.DeserializeAsync<LocalModuleResponseWrapper>(responseContent);
                    
        if (wrapper?.Results != null && wrapper.Results.Count > 0)
            status = wrapper.Results[0];
        
        if (status == null)
            return new CheckMarksDataTrueApi();

        if (status.Code == 0) 
            return status;
        
        _logger.LogWarning("Ошибка в ответе от локального модуля: {err}", status.Description);
        return new CheckMarksDataTrueApi();
    }

    private async Task<bool> EniseyOnline(string address)
    {
        if (address == string.Empty)
            return true;
            
        using var httpClientEnisey = _httpClientFactory.CreateClient("Enisey");
        httpClientEnisey.BaseAddress = new Uri(address);

        try
        {
            var eniseyResponse = await httpClientEnisey.GetAsync("");

            if (!eniseyResponse.IsSuccessStatusCode)
                return false;
        }
        catch
        {
            return false;
        }

        return true;
    }
}

internal class LocalModuleResponseWrapper
{
    [System.Text.Json.Serialization.JsonPropertyName("results")]
    public List<CheckMarksDataTrueApi> Results { get; set; } = [];
}