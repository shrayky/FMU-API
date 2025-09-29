using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Cdn;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Shared.Http;
using System.Net.Http.Json;
using TrueApi.Interface;
using TrueApiCdn.Interface;

namespace TrueApi.Services
{
    [AutoRegisterService(ServiceLifetime.Scoped)]
    public class TrueApiClientService : ITrueApiClientService
    {
        private ILogger<TrueApiClientService> _logger;
        private IHttpClientFactory _httpClientFactory;
        private readonly ICdnService _cdnService;

        private readonly string _address = "/api/v4/true-api";

        public TrueApiClientService(ILogger<TrueApiClientService> logger, IHttpClientFactory httpClientFactory, ICdnService cdnService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cdnService = cdnService;
        }

        public async Task<Result> HaveActiveCdns()
        {
            IReadOnlyList<TrueSignCdn> cdns = await _cdnService.GetCdnsAsync();

            if (cdns.Count == 0)
                return Result.Failure<CheckMarksDataTrueApi>("Нет загруженных CDN");

            TrueSignCdn? cdn = await _cdnService.GetActiveCdnAsync(0);

            if (cdn is null)
                return Result.Failure<CheckMarksDataTrueApi>("Нет активных CDN");

            return Result.Success();
        }

        public async Task<Result<CheckMarksDataTrueApi>> MarksOnLineCheck(CheckMarksRequestData marksRequestData, string xApiKey, TimeSpan timeoutInSeconds)
        {
            var content = JsonContent.Create(marksRequestData);

            var cdn = await _cdnService.GetActiveCdnAsync(0);
            
            if (cdn is null)
                return Result.Failure<CheckMarksDataTrueApi>("Нет активных CDN");

            _logger.LogInformation("Проверяю марки в честном знаке {@request}", content);

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            httpClient.DefaultRequestHeaders.Add("X-API-KEY", xApiKey);
            httpClient.Timeout = timeoutInSeconds;
            
            var markCheckResult = await httpClient.SendRequestSafelyAsync(
                    client => client.PostAsync($"{cdn.Host}{_address}/codes/check", content),
                    _logger,
                    "проверка марок в Честном знаке");

            if (markCheckResult.IsFailure)
            {
                var err = $"CDN {cdn.Host} не ответил корректно за {timeoutInSeconds} секунды, помечаем его off-line";
                _logger.LogWarning(err);
                cdn.BringOffline();

                return Result.Failure<CheckMarksDataTrueApi>(err);
            }

            var answer = await markCheckResult.Value.Content.ReadFromJsonAsync<CheckMarksDataTrueApi>();

            if (answer is null)
            {
                var err = $"Пустой ответ от CDN {cdn.Host}, помечаем его off-line";
                cdn.BringOffline();

                return Result.Failure<CheckMarksDataTrueApi>(err);
            }

            _logger.LogInformation("Получен ответ от честного знака {@answer}", answer);
            return Result.Success(answer);
        }
    }
}
