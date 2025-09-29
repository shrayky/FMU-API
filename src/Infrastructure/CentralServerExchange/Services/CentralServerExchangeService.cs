using System.Net.Http.Json;
using CentralServerExchange.Dto.Answer;
using CentralServerExchange.Dto.Request;
using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CentralServerExchange.Services
{
    [AutoRegisterService(ServiceLifetime.Singleton)]
    public class CentralServerExchangeService : IExchangeService
    {
        private HttpClient HttpClient { get; init; }
        private ILogger<CentralServerExchangeService> Logger { get; init; }

        public CentralServerExchangeService(ILogger<CentralServerExchangeService> logger, HttpClient httpClient)
        {
            HttpClient = httpClient;
            Logger = logger;
        }

        public async Task<Result<FmuApiCentralResponse>> ActExchange(DataPacket request, string url) 
            => await SafeActExchange(request, url);
      
        private async Task<Result<FmuApiCentralResponse>> SafeActExchange(DataPacket request,  string url)
        {
            Logger.LogInformation("Готовлю к отправке пакет информации на сервер: {Url}", url);
            
            try
            {
                var response = await HttpClient.PostAsJsonAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Logger.LogError("Сервер вернул ошибку: {StatusCode}, {Error}",
                        response.StatusCode, error);

                    return Result.Failure<FmuApiCentralResponse>(
                        $"Сервер вернул ошибку {response.StatusCode}: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<FmuApiCentralResponse>();
                
                if (result is null)
                {
                    return Result.Failure<FmuApiCentralResponse>("Пустой ответ от сервера");
                }

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Обмен с центральным сервером закончился неудачно");
                return Result.Failure<FmuApiCentralResponse>($"Обмен с центральным сервером закончился с ошибкой: {ex.Message}");
            }
        }


    }
}