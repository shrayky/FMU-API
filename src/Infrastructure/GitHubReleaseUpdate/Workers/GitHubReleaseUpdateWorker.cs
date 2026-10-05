using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Constants;
using FmuApiDomain.Files;
using FmuApiDomain.SoftwareUpdate;
using FmuApiDomain.SoftwareUpdate.Interfaces;
using GitHubReleaseUpdate.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace GitHubReleaseUpdate.Workers;

/// Проверяет последний релиз GitHub и устанавливает обновление.
public class GitHubReleaseUpdateWorker : BackgroundService
{
    private const string ChecksumFileName = "checksum.txt";

#if DEBUG
    private const int StartDelayMinutes = 1;
#else
    private const int StartDelayMinutes = 5;
#endif

    private readonly ILogger<GitHubReleaseUpdateWorker> _logger;
    private readonly IParametersService _parametersService;
    private readonly GitHubReleaseClient _releaseClient;
    private readonly ISoftwarePackageInstaller _packageInstaller;

    private DateTime _nextCheckTime;

    public GitHubReleaseUpdateWorker(
        ILogger<GitHubReleaseUpdateWorker> logger,
        IParametersService parametersService,
        GitHubReleaseClient releaseClient,
        ISoftwarePackageInstaller packageInstaller)
    {
        _logger = logger;
        _parametersService = parametersService;
        _releaseClient = releaseClient;
        _packageInstaller = packageInstaller;

        _nextCheckTime = DateTime.Now.AddMinutes(StartDelayMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (DateTime.Now < _nextCheckTime)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
                continue;
            }

            var configuration = await _parametersService.CurrentAsync().ConfigureAwait(false);
            var options = configuration.GitHubReleaseUpdate;

            try
            {
                if (options.Enabled)
                    await CheckAndInstallAsync(options, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Проверка обновления из релиза GitHub не удалась");
            }

            var intervalMinutes = Math.Max(1, options.CheckIntervalMinutes);
            _nextCheckTime = DateTime.Now.AddMinutes(intervalMinutes);

            _logger.LogInformation(
                "Проверка релиза GitHub: следующая проверка запланирована на {NextCheckTime}",
                _nextCheckTime);
        }
    }

    private async Task CheckAndInstallAsync(GitHubReleaseUpdateOptions options, CancellationToken cancellationToken)
    {
        // Вне окна расписания установки запрос к GitHub не тратим.
        if (!UpdateInstallSchedule.IsWithinSchedule(TimeOnly.FromDateTime(DateTime.Now), options.InstallSchedule))
        {
            _logger.LogInformation("Обновление отложено: текущее время вне разрешённых интервалов");
            return;
        }

        _logger.LogInformation("Проверяю последний релиз GitHub");

        var assetsResult = await _releaseClient
            .GetLatestReleaseAssetsAsync(cancellationToken)
            .ConfigureAwait(false);

        if (assetsResult.IsFailure)
        {
            _logger.LogError("Не удалось получить последний релиз GitHub: {Error}", assetsResult.Error);
            return;
        }

        var asset = SoftwarePackageSelector.SelectLatest(
            assetsResult.Value,
            ApplicationInformation.AppVersion,
            ApplicationInformation.Assembly,
            SoftwarePackageSelector.CurrentArchitecture(),
            SoftwarePackageSelector.CurrentOperatingSystem());

        if (asset is null)
        {
            _logger.LogInformation("В последнем релизе нет архива новее текущей версии для этой ОС и архитектуры");
            return;
        }

        _logger.LogInformation("Найден архив обновления {AssetName}", asset.Name);

        var downloadResult = await _releaseClient
            .DownloadAssetAsync(asset, cancellationToken)
            .ConfigureAwait(false);

        if (downloadResult.IsFailure)
        {
            _logger.LogError("Не удалось скачать архив обновления: {Error}", downloadResult.Error);
            return;
        }

        var sha256 = await ComputeSha256Async(downloadResult.Value, cancellationToken).ConfigureAwait(false);

        if (IsAlreadyInstalled(sha256))
        {
            _logger.LogInformation("Пакет {AssetName} уже установлен, установка не требуется", asset.Name);
            return;
        }

        var installResult = await _packageInstaller
            .InstallAsync(downloadResult.Value, sha256)
            .ConfigureAwait(false);

        if (installResult.IsFailure)
        {
            _logger.LogError("Установка обновления не удалась: {Error}", installResult.Error);
            return;
        }

        _logger.LogInformation("Обновление {AssetName} установлено", asset.Name);
    }

    /// Совпадение с checksum.txt означает, что этот пакет уже установлен.
    private static bool IsAlreadyInstalled(string sha256)
    {
        var checksumPath = Path.Combine(ServiceFolders.DataFolder(), ChecksumFileName);

        if (!File.Exists(checksumPath))
            return false;

        var installedChecksum = File.ReadAllText(checksumPath).Trim();

        return string.Equals(installedChecksum, sha256, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(fileStream, cancellationToken).ConfigureAwait(false);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
