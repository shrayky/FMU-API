using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Constants;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.CentralServiceExchange.Models.DataPacket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class CentralServerExchangeService : IExchangeService
{
    public const string HttpClientName = "CentralServerExchange";
    public const string DownloadHttpClientName = "CentralServerExchangeDownload";

    private const long MaxUpdateSizeBytes = 200L * 1024 * 1024;
    private static readonly TimeSpan DownloadInactivityTimeout = TimeSpan.FromMinutes(2);

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

    public async Task<Result<string>> DownloadSoftwareUpdateToTemp(string requestAddress, string sha256)
    {
        var tmpFolder = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName, "updates");
        var tmpPath = Path.Combine(tmpFolder, $"update_{sha256}.partial");

        try
        {
            Directory.CreateDirectory(tmpFolder);

            var existingLength = GetExistingFileLength(tmpPath);
            if (existingLength > MaxUpdateSizeBytes)
            {
                TryDeleteFile(tmpPath);
                existingLength = 0;
            }

            using var inactivityCts = new CancellationTokenSource();
            inactivityCts.CancelAfter(DownloadInactivityTimeout);

            var httpClient = _httpClientFactory.CreateClient(DownloadHttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestAddress);
            if (existingLength > 0)
            {
                request.Headers.Range = new RangeHeaderValue(existingLength, null);
                _logger.LogInformation("Докачка обновления с позиции {Offset} байт", existingLength);
            }

            using var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, inactivityCts.Token)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
            {
                if (existingLength > 0)
                {
                    _logger.LogInformation("Сервер вернул 416, используем уже скачанный файл {FilePath}", tmpPath);
                    return Result.Success(tmpPath);
                }

                return Result.Failure<string>("Сервер вернул 416 Requested Range Not Satisfiable");
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(inactivityCts.Token).ConfigureAwait(false);
                return Result.Failure<string>($"Сервер вернул ошибку {response.StatusCode}: {error}");
            }

            var append = response.StatusCode == HttpStatusCode.PartialContent && existingLength > 0;
            if (append && !CanAppendPartialContent(response, existingLength))
            {
                TryDeleteFile(tmpPath);
                return Result.Failure<string>("Некорректный Content-Range, файл удалён для повторной загрузки");
            }

            if (append && !File.Exists(tmpPath))
            {
                return Result.Failure<string>("Частичный файл исчез до докачки, повторная загрузка начнётся с начала");
            }

            if (!append && existingLength > 0)
            {
                _logger.LogInformation("Сервер не поддерживает Range, скачиваем файл заново");
                existingLength = 0;
            }

            var expectedTotal = GetExpectedTotalBytes(response, existingLength, append);
            if (expectedTotal.HasValue && expectedTotal.Value > MaxUpdateSizeBytes)
            {
                TryDeleteFile(tmpPath);
                return Result.Failure<string>(
                    $"Файл обновления превышает лимит {MaxUpdateSizeBytes} байт (ожидается {expectedTotal.Value})");
            }

            _logger.LogInformation(
                "Загружаем файл обновления. Content-Type: {ContentType}, Size: {Size}, Append: {Append}",
                response.Content.Headers.ContentType?.MediaType,
                expectedTotal,
                append);

            inactivityCts.CancelAfter(DownloadInactivityTimeout);

            await using var contentStream = await response.Content
                .ReadAsStreamAsync(inactivityCts.Token)
                .ConfigureAwait(false);

            var copyResult = await CopyStreamToFileAsync(
                    contentStream,
                    tmpPath,
                    append,
                    existingLength,
                    inactivityCts)
                .ConfigureAwait(false);

            if (copyResult.SizeExceeded)
            {
                TryDeleteFile(tmpPath);
                return Result.Failure<string>(
                    $"Файл обновления превышает лимит {MaxUpdateSizeBytes} байт");
            }

            if (expectedTotal.HasValue && copyResult.Total < expectedTotal.Value)
            {
                _logger.LogWarning(
                    "Загрузка прервана: получено {Actual} из {Expected} байт, файл сохранён для докачки",
                    copyResult.Total,
                    expectedTotal.Value);
                return Result.Failure<string>(
                    $"Загрузка не завершена: {copyResult.Total} из {expectedTotal.Value} байт");
            }

            _logger.LogInformation("Обновление загружено в: {FilePath}, размер {Size}", tmpPath, copyResult.Total);
            return Result.Success(tmpPath);
        }
        catch (OperationCanceledException)
        {
            var errMsg = "Загрузка обновления прервана по таймауту простоя, файл сохранён для докачки";
            _logger.LogWarning(errMsg);
            return Result.Failure<string>(errMsg);
        }
        catch (Exception e)
        {
            var errMsg = $"Ошибка загрузки обновления в temp-файл {tmpPath}: {e.Message}. Файл сохранён для докачки";
            _logger.LogError(errMsg);
            return Result.Failure<string>(errMsg);
        }
    }

    private static bool CanAppendPartialContent(HttpResponseMessage response, long existingLength)
    {
        var from = response.Content.Headers.ContentRange?.From;
        return from.HasValue && from.Value == existingLength;
    }

    private static long? GetExpectedTotalBytes(HttpResponseMessage response, long existingLength, bool append)
    {
        if (append)
        {
            if (response.Content.Headers.ContentRange?.Length is long total)
                return total;

            if (response.Content.Headers.ContentLength is long remaining)
                return existingLength + remaining;

            return null;
        }

        return response.Content.Headers.ContentLength;
    }

    private static async Task<(bool SizeExceeded, long Total)> CopyStreamToFileAsync(
        Stream contentStream,
        string tmpPath,
        bool append,
        long existingLength,
        CancellationTokenSource inactivityCts)
    {
        var fileMode = append ? FileMode.Append : FileMode.Create;
        await using var fileStream = new FileStream(tmpPath, fileMode, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long total = append ? existingLength : 0;
        int read;

        while ((read = await contentStream
                   .ReadAsync(buffer.AsMemory(0, buffer.Length), inactivityCts.Token)
                   .ConfigureAwait(false)) > 0)
        {
            inactivityCts.CancelAfter(DownloadInactivityTimeout);

            total += read;
            if (total > MaxUpdateSizeBytes)
                return (true, total);

            await fileStream
                .WriteAsync(buffer.AsMemory(0, read), inactivityCts.Token)
                .ConfigureAwait(false);
        }

        return (false, total);
    }

    private static long GetExistingFileLength(string path)
    {
        if (!File.Exists(path))
            return 0;

        return new FileInfo(path).Length;
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
