using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

/// <summary>
/// Общие операции HTTP для клиентов ГИС МТ TrueAPI.
/// </summary>
internal static class GisMtTrueApiHttp
{
    public const string HttpClientName = "GisMtTrueApi";
    public const string BaseUrl = "https://markirovka.crpt.ru";

    public static HttpRequestMessage CreateRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    public static Task<Result<T>> ReadJsonOrFailure<T>(
        HttpResponseMessage response,
        string operationName,
        CancellationToken cancellationToken)
    {
        return ReadJsonOrFailure<T>(response, operationName, null, cancellationToken);
    }

    public static async Task<Result<T>> ReadJsonOrFailure<T>(
        HttpResponseMessage response,
        string operationName,
        JsonSerializerOptions? jsonSerializerOptions,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<T>($"{operationName} HTTP {(int)response.StatusCode}: {errorBody}");
        }

        var data = await response.Content.ReadFromJsonAsync<T>(jsonSerializerOptions, cancellationToken);
        if (data is null)
            return Result.Failure<T>($"Пустой ответ {operationName}");

        return Result.Success(data);
    }

    public static Result<T> FailLogged<T>(ILogger logger, Exception ex, string message, params object[] args)
    {
        logger.LogError(ex, message, args);
        return Result.Failure<T>(ex.Message);
    }
}
