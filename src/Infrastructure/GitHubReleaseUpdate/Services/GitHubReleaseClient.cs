using CSharpFunctionalExtensions;
using FmuApiDomain.Constants;
using FmuApiDomain.SoftwareUpdate.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitHubReleaseUpdate.Services;

/// Клиент GitHub: метаданные последнего релиза и скачивание архива.
public class GitHubReleaseClient
{
    public const string HttpClientName = "GitHubRelease";
    public const string DownloadHttpClientName = "GitHubReleaseDownload";

    public const string UserAgent = "FMU-API";

    private static readonly JsonSerializerOptions ResponseOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubReleaseClient> _logger;

    public GitHubReleaseClient(IHttpClientFactory httpClientFactory, ILogger<GitHubReleaseClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<Result<IReadOnlyList<ReleaseAsset>>> GetLatestReleaseAssetsAsync(CancellationToken cancellationToken)
        => GetLatestReleaseAssets(ApplicationInformation.GitHubReleaseMetadataUrl, cancellationToken);

    public async Task<Result<IReadOnlyList<ReleaseAsset>>> GetLatestReleaseAssets(
        string metadataUrl,
        CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client
            .GetAsync(metadataUrl, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<IReadOnlyList<ReleaseAsset>>(
                $"GitHub вернул код {(int)response.StatusCode} на запрос последнего релиза");

        await using var contentStream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        var release = await JsonSerializer
            .DeserializeAsync<GitHubReleaseResponse>(contentStream, ResponseOptions, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<ReleaseAsset>>(release?.Assets ?? []);
    }

    public Task<Result<string>> DownloadAssetAsync(ReleaseAsset asset, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(asset.Name);
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult(Result.Failure<string>($"Некорректное имя файла в релизе: {asset.Name}"));

        return DownloadTo(asset, Path.Combine(UpdatesFolder(), fileName), cancellationToken);
    }

    public Task<Result<string>> DownloadTo(
        ReleaseAsset asset,
        string destinationPath,
        CancellationToken cancellationToken)
        => DownloadTo(asset, destinationPath, maxBytes: null, cancellationToken);

    public async Task<Result<string>> DownloadTo(
        ReleaseAsset asset,
        string destinationPath,
        long? maxBytes,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            return Result.Failure<string>($"У файла {asset.Name} нет адреса для скачивания");

        var directory = Path.GetDirectoryName(destinationPath);
        if (string.IsNullOrWhiteSpace(directory))
            return Result.Failure<string>($"Некорректный путь сохранения {destinationPath}");

        Directory.CreateDirectory(directory);
        var partPath = destinationPath + ".part";

        try
        {
            using var client = _httpClientFactory.CreateClient(DownloadHttpClientName);
            using var response = await client
                .GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<string>(
                    $"GitHub вернул код {(int)response.StatusCode} на скачивание {asset.Name}");

            _logger.LogInformation("Скачиваю {AssetName}", asset.Name);

            if (maxBytes is long advertisedLimit
                && response.Content.Headers.ContentLength is long contentLength
                && contentLength > advertisedLimit)
            {
                return Result.Failure<string>($"Файл {asset.Name} больше {advertisedLimit} байт");
            }

            var tooLarge = false;
            await using (var contentStream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false))
            await using (var fileStream = File.Create(partPath))
            {
                var buffer = new byte[81920];
                long total = 0;
                while (true)
                {
                    var read = await contentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        break;

                    total += read;
                    if (maxBytes is long limit && total > limit)
                    {
                        tooLarge = true;
                        break;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }

            if (tooLarge)
            {
                DeletePart(partPath);
                return Result.Failure<string>($"Файл {asset.Name} больше {maxBytes} байт");
            }

            File.Move(partPath, destinationPath, overwrite: true);
            return Result.Success(destinationPath);
        }
        catch (OperationCanceledException)
        {
            DeletePart(partPath);
            throw;
        }
        catch (Exception exception)
        {
            DeletePart(partPath);
            return Result.Failure<string>(exception.Message);
        }
    }

    private static void DeletePart(string partPath)
    {
        if (File.Exists(partPath))
            File.Delete(partPath);
    }

    public static string UpdatesFolder() =>
        Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName, "updates");

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("assets")]
        public List<ReleaseAsset> Assets { get; set; } = [];
    }
}
