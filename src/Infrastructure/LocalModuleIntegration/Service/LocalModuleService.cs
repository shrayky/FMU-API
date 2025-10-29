using FmuApiDomain.Configuration.Options;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.TrueApi.MarkData.Check;
using LocalModuleIntegration.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace LocalModuleIntegration.Service
{
    public class LocalModuleService : ILocalModuleService
    {
        private readonly ILogger<LocalModuleService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string InitAddress = @"/api/v1/init";
        private const string StatusAddress = @"/api/v1/status";
        private const string OutCheckAddress = @"/api/v1/cis/outCheck";

        public LocalModuleService(IHttpClientFactory httpClientFactory, ILogger<LocalModuleService> logger)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> InitializeAsync(LocalModuleConnection connection, string xApiKey)
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
                token = xApiKey
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

        public async Task<CheckMarksDataTrueApi> OutCheckAsync(LocalModuleConnection connection, string cis, string xapiKey)
        {
            using var httpClient = _httpClientFactory.CreateClient("LocalModule");

            httpClient.BaseAddress = new Uri(connection.ConnectionAddress);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", connection.GetBasicAuthorizationHeader());

            var encodedCis = Uri.EscapeDataString(cis);

            var address = $"{OutCheckAddress}?cis={encodedCis}";

            var response = await httpClient.GetAsync(address);

            var responseContent = await response.Content.ReadAsStringAsync();

            var status = await JsonHelpers.DeserializeAsync<CheckMarksDataTrueApi>(await response.Content.ReadAsStreamAsync());

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var err = status == null ? responseContent : status.Description;
                _logger.LogWarning("Ошибка в ответе от локального модуля: {err}", err);
            }

            if (status == null)
                return new();

            if (status.Code != 0)
            {
                _logger.LogWarning("Ошибка в ответе от локального модуля: {err}", status.Description);
                return new CheckMarksDataTrueApi();
            }

            return status;
        }

        public async Task<LocalModuleState> StateAsync(LocalModuleConnection connection)
        {
            if (!connection.Enable)
                return new();

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

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Ошибка получения статуса ЛМ. Код: {StatusCode}, Причина: {ReasonPhrase}, Тело: {ErrorContent}",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    errorContent
                );
                
                return new();
            }

            return state ?? new();
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
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

    }
}
