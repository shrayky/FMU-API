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

        private string initAddress = @"/api/v1/init";
        private string statusAddress = @"/api/v1/status";
        private string outCheckAddress = @"/api/v1/cis/outCheck";

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

            var response = await httpClient.PostAsJsonAsync(initAddress, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Ошибка при инициализации ЛМ. Код: {StatusCode}, Причина: {ReasonPhrase}, Тело: {ErrorContent}",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    errorContent
                );
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<CheckMarksDataTrueApi> OutCheckAsync(LocalModuleConnection connection, string cis, string xapiKey)
        {
            using var httpClient = _httpClientFactory.CreateClient("LocalModule");

            httpClient.BaseAddress = new Uri(connection.ConnectionAddress);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", connection.GetBasicAuthorizationHeader());

            string address = $"{outCheckAddress}?cis={cis}";

            var response = await httpClient.GetAsync(address);

            var responseContent = await response.Content.ReadAsStringAsync();

            var status = await JsonHelpers.DeserializeAsync<CheckMarksDataTrueApi>(await response.Content.ReadAsStreamAsync());

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {

            } 

            return status;
        }

        public async Task<LocalModuleState> StateAsync(LocalModuleConnection connection)
        {
            if (!connection.Enable)
            {
                _logger.LogWarning("Попытка инициализации отключенного ЛМ");
                return new();
            }

            using var httpClient = _httpClientFactory.CreateClient("LocalModule");

            httpClient.BaseAddress = new Uri(connection.ConnectionAddress);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", connection.GetBasicAuthorizationHeader());

            var response = await httpClient.GetAsync(statusAddress);

            var responseContent = await response.Content.ReadAsStringAsync();

            var status = await JsonHelpers.DeserializeAsync<LocalModuleState>(await response.Content.ReadAsStreamAsync());

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Ошибка при инициализации ЛМ. Код: {StatusCode}, Причина: {ReasonPhrase}, Тело: {ErrorContent}",
                    (int)response.StatusCode,
                    response.ReasonPhrase,
                    errorContent
                );
                return new();

            }

            return status ?? new();
        }

    }
}
