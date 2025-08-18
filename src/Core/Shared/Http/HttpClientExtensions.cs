using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;

namespace Shared.Http
{
    public static class HttpClientExtensions
    {
        public static Result<HttpClient, string> CreateClientSafely(
            this IHttpClientFactory httpClientFactory,
            string clientName,
            ILogger logger)
        {
            try
            {
                var client = httpClientFactory.CreateClient(clientName);
                return Result.Success<HttpClient, string>(client);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Клиент '{ClientName}' не зарегистрирован в DI", clientName);
                return Result.Failure<HttpClient, string>($"Клиент '{clientName}' не зарегистрирован");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Неожиданная ошибка при создании HttpClient '{ClientName}'", clientName);
                return Result.Failure<HttpClient, string>($"Ошибка создания клиента: {ex.Message}");
            }
        }

        public static async Task<Result<HttpResponseMessage, string>> SendRequestSafelyAsync(
            this HttpClient httpClient,
            Func<HttpClient, Task<HttpResponseMessage>> requestFunc,
            ILogger logger,
            string operationName = "HTTP запрос")
        {
            try
            {
                var response = await requestFunc(httpClient);
                return Result.Success<HttpResponseMessage, string>(response);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Сетевая ошибка при выполнении {OperationName}", operationName);
                return Result.Failure<HttpResponseMessage, string>($"Сетевая ошибка: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                logger.LogError(ex, "Таймаут при выполнении {OperationName}", operationName);
                return Result.Failure<HttpResponseMessage, string>("Превышено время ожидания");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Неожиданная ошибка при выполнении {OperationName}", operationName);
                return Result.Failure<HttpResponseMessage, string>($"Ошибка запроса: {ex.Message}");
            }
        }
    }
}