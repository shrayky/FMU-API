using System.Net.Http.Json;
using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.DTO.FmuApiExchangeData.Answer;
using FmuApiDomain.DTO.FmuApiExchangeData.Request;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Http;

namespace CentralServerExchange.Services
{
    [AutoRegisterService(ServiceLifetime.Singleton)]
    public class CentralServerExchangeService : IExchangeService
    {
        private HttpClient HttpClient { get; init; }
        private ILogger<CentralServerExchangeService> Logger { get; init; }

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(600);

        public CentralServerExchangeService(ILogger<CentralServerExchangeService> logger, HttpClient httpClient)
        {
            HttpClient = httpClient;
            Logger = logger;
        }

        public async Task<Result<FmuApiCentralResponse>> ActExchange(DataPacket request, string url) 
            => await SafeActExchange(request, url).ConfigureAwait(false);

        private async Task<Result<FmuApiCentralResponse>> SafeActExchange(DataPacket request,  string url)
        {
            Logger.LogInformation("Готовлю к отправке пакет информации на сервер: {Url}", url);
            
            try
            {
                var response = await HttpClient.PostAsJsonAsync(url, request).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return Result.Failure<FmuApiCentralResponse>(
                        $"Сервер вернул ошибку {response.StatusCode}: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<FmuApiCentralResponse>().ConfigureAwait(false);
                
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
    
        public async Task<Result<string>> DownloadNewConfiguration(string url)
        {
            var operationResult = await HttpClient.SendRequestSafelyAsync(
                client => client.GetAsync(url),
                Logger,
                "загрузка настроек из центрального сервера").ConfigureAwait(false);
            
            if (operationResult.IsFailure)
                return Result.Failure<string>(operationResult.Error);
        
            var content = await operationResult.Value.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            return Result.Success(content);
        }
        public async Task<Result> ConfirmDownloadConfiguration(string url)
        {
            var operationResult = await HttpClient.SendRequestSafelyAsync(
                client => client.PutAsJsonAsync(url, new {}),
                Logger,
                "уведомление о загрузке настроек").ConfigureAwait(false);
            
            return operationResult.IsSuccess ? Result.Success() : Result.Failure(operationResult.Error); 
        }

        public async Task<Result<Stream>> DownloadSoftwareUpdate(string requestAddress)
        {
            var operationResult = await HttpClient.SendRequestSafelyAsync(
                client => client.GetAsync(requestAddress),
                Logger,
                "загрузка обновления программного обеспечения").ConfigureAwait(false);
    
            if (operationResult.IsFailure)
                return Result.Failure<Stream>(operationResult.Error);
    
            var response = operationResult.Value;
    
            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength.HasValue && contentLength.Value > 100 * 1024 * 1024) // 100MB
            {
                Logger.LogWarning("Файл обновления очень большой: {Size} байт", contentLength.Value);
            }
    
            var contentType = response.Content.Headers.ContentType?.MediaType;
            Logger.LogInformation("Загружаем файл обновления. Content-Type: {ContentType}, Size: {Size}", 
                contentType, contentLength);
    
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            return Result.Success(stream);
        }
    }
}