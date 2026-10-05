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

    public async Task<Result<IReadOnlyList<ReleaseAsset>>> GetLatestReleaseAssetsAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await client
            .GetAsync(ApplicationInformation.GitHubReleaseMetadataUrl, cancellationToken)
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

    public async Task<Result<string>> DownloadAssetAsync(ReleaseAsset asset, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            return Result.Failure<string>($"У файла {asset.Name} нет адреса для скачивания");

        var updatesFolder = UpdatesFolder();
        Directory.CreateDirectory(updatesFolder);

        var fileName = Path.GetFileName(asset.Name);
        if (string.IsNullOrWhiteSpace(fileName))
            return Result.Failure<string>($"Некорректное имя файла в релизе: {asset.Name}");

        var destinationPath = Path.Combine(updatesFolder, fileName);

        using var client = _httpClientFactory.CreateClient(DownloadHttpClientName);
        using var response = await client
            .GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<string>(
                $"GitHub вернул код {(int)response.StatusCode} на скачивание {asset.Name}");

        _logger.LogInformation("Скачиваю архив обновления {AssetName}", asset.Name);

        await using var contentStream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        await using var fileStream = File.Create(destinationPath);

        await contentStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

        return Result.Success(destinationPath);
    }

    public static string UpdatesFolder() =>
        Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName, "updates");

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("assets")]
        public List<ReleaseAsset> Assets { get; set; } = [];
    }
}
