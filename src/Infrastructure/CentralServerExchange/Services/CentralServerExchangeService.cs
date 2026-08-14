using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Constants;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.CentralServiceExchange.Models.DataPacket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Http;
using System.Net.Http.Json;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class CentralServerExchangeService : IExchangeService
{
    public const string HttpClientName = "CentralServerExchange";

    private const long MaxUpdateSizeBytes = 100L * 1024 * 1024;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CentralServerExchangeService> _logger;

    public CentralServerExchangeService(ILogger<CentralServerExchangeService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<FmuApiCentralResponse>> ActExchange(DataPacket request, string url)
        => await SafeActExchange(request, url).ConfigureAwait(false);

    private HttpClient CreateClient() => _httpClientFactory.CreateClient(HttpClientName);

    private async Task<Result<FmuApiCentralResponse>> SafeActExchange(DataPacket request, string url)
    {
        _logger.LogInformation("Готовлю к отправке пакет информации на сервер: {Url}", url);

        try
        {
            var httpClient = CreateClient();
            using var response = await httpClient.PostAsJsonAsync(url, request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return Result.Failure<FmuApiCentralResponse>(
                    $"Сервер вернул ошибку {response.StatusCode}: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<FmuApiCentralResponse>().ConfigureAwait(false);

            if (result is null)
                return Result.Failure<FmuApiCentralResponse>("Пустой ответ от сервера");

            if (!result.Success)
            {
                var message = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Центральный сервер вернул Success=false"
                    : result.ErrorMessage;

                return Result.Failure<FmuApiCentralResponse>(message);
            }

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Обмен с центральным сервером закончился неудачно: {Message}", ex.Message);
            _logger.LogDebug(ex, "Детали ошибки обмена с центральным сервером");
            return Result.Failure<FmuApiCentralResponse>($"Обмен с центральным сервером закончился с ошибкой: {ex.Message}");
        }
    }

    public async Task<Result<string>> DownloadNewConfiguration(string url)
    {
        var httpClient = CreateClient();
        var operationResult = await httpClient.SendRequestSafelyAsync(
            client => client.GetAsync(url),
            _logger,
            "загрузка настроек из центрального сервера").ConfigureAwait(false);

        if (operationResult.IsFailure)
            return Result.Failure<string>(operationResult.Error);

        using var response = operationResult.Value;

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Result.Failure<string>($"Сервер вернул ошибку {response.StatusCode}: {error}");
        }

        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return Result.Success(content);
    }

    public async Task<Result> ConfirmDownloadConfiguration(string url)
    {
        var httpClient = CreateClient();
        var operationResult = await httpClient.SendRequestSafelyAsync(
            client => client.PutAsJsonAsync(url, new { }),
            _logger,
            "уведомление о загрузке настроек").ConfigureAwait(false);

        if (operationResult.IsFailure)
            return Result.Failure(operationResult.Error);

        using var response = operationResult.Value;

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Result.Failure($"Сервер вернул ошибку {response.StatusCode}: {error}");
        }

        return Result.Success();
    }

    public async Task<Result<string>> DownloadSoftwareUpdateToTemp(string requestAddress)
    {
        var httpClient = CreateClient();
        var operationResult = await httpClient.SendRequestSafelyAsync(
            client => client.GetAsync(requestAddress, HttpCompletionOption.ResponseHeadersRead),
            _logger,
            "загрузка обновления программного обеспечения").ConfigureAwait(false);

        if (operationResult.IsFailure)
            return Result.Failure<string>(operationResult.Error);

        using var response = operationResult.Value;

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Result.Failure<string>($"Сервер вернул ошибку {response.StatusCode}: {error}");
        }

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > MaxUpdateSizeBytes)
        {
            return Result.Failure<string>(
                $"Файл обновления превышает лимит {MaxUpdateSizeBytes} байт (Content-Length={contentLength.Value})");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;

        _logger.LogInformation("Загружаем файл обновления. Content-Type: {ContentType}, Size: {Size}",
            contentType, contentLength);

        var tmpFolder = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName, "updates");
        var tmpFile = $"update_{Guid.NewGuid():N}.zip";
        var tmpPath = Path.Combine(tmpFolder, tmpFile);

        try
        {
            Directory.CreateDirectory(tmpFolder);

            await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var sizeExceeded = false;
            await using (var fileStream = File.Create(tmpPath))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;

                while ((read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false)) > 0)
                {
                    total += read;
                    if (total > MaxUpdateSizeBytes)
                    {
                        sizeExceeded = true;
                        break;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                }
            }

            if (sizeExceeded)
            {
                TryDeleteFile(tmpPath);
                return Result.Failure<string>(
                    $"Файл обновления превышает лимит {MaxUpdateSizeBytes} байт");
            }

            _logger.LogInformation("Обновление загружено в: {FilePath}", tmpPath);
            return Result.Success(tmpPath);
        }
        catch (Exception e)
        {
            TryDeleteFile(tmpPath);

            var errMsg = $"Ошибка загрузки обновления в temp-файл {tmpPath}: {e.Message}";
            _logger.LogError(errMsg);
            return Result.Failure<string>(errMsg);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
