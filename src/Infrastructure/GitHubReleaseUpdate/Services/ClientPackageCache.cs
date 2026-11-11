using CSharpFunctionalExtensions;
using FmuApiDomain.SoftwareUpdate;
using FmuApiDomain.SoftwareUpdate.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace GitHubReleaseUpdate.Services;

/// Скачивает пакет указанного клиентского приложения из последнего релиза GitHub и принимает APK с диска.
public class ClientPackageCache
{
    private readonly GitHubReleaseClient _releaseClient;
    private readonly ILogger<ClientPackageCache> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ClientPackageCache(GitHubReleaseClient releaseClient, ILogger<ClientPackageCache> logger)
    {
        _releaseClient = releaseClient;
        _logger = logger;
    }

    public async Task<Result<ClientPackageRefreshResult>> Refresh(
        string application,
        string repositoryAddress,
        CancellationToken cancellationToken)
    {
        if (!ClientPackageName.IsApplicationId(application))
            return Result.Failure<ClientPackageRefreshResult>("Некорректное имя приложения");

        if (!GitHubReleaseAddress.TryMetadataUrl(repositoryAddress, out var metadataUrl))
            return Result.Failure<ClientPackageRefreshResult>("Некорректный адрес репозитория GitHub");

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await DownloadSelected(application, metadataUrl, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<Result<ClientPackageRefreshResult>> Store(
        string application,
        IReadOnlyList<ClientPackageUpload> uploads,
        CancellationToken cancellationToken)
    {
        if (!ClientPackageName.IsApplicationId(application))
            return Result.Failure<ClientPackageRefreshResult>("Некорректное имя приложения");

        if (uploads.Count == 0)
            return Result.Failure<ClientPackageRefreshResult>("Не выбран файл APK");

        if (uploads.Count > 2)
            return Result.Failure<ClientPackageRefreshResult>("Можно загрузить не больше двух APK");

        var folder = ClientPackageCatalog.ApplicationFolder(application);
        if (folder is null)
            return Result.Failure<ClientPackageRefreshResult>("Некорректное имя приложения");

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(folder);
            var downloaded = 0;
            var errors = new List<string>();

            foreach (var upload in uploads)
            {
                var error = await SaveUpload(folder, application, upload, cancellationToken).ConfigureAwait(false);
                if (error is null)
                    downloaded++;
                else
                    errors.Add(error);
            }

            return Result.Success(new ClientPackageRefreshResult(downloaded, errors));
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<Result<ClientPackageRefreshResult>> DownloadSelected(
        string application,
        string metadataUrl,
        CancellationToken cancellationToken)
    {
        var assetsResult = await _releaseClient
            .GetLatestReleaseAssets(metadataUrl, cancellationToken)
            .ConfigureAwait(false);

        if (assetsResult.IsFailure)
            return Result.Failure<ClientPackageRefreshResult>(assetsResult.Error);

        var selected = ClientPackageSelector.SelectApplication(assetsResult.Value, application);
        if (selected.Count == 0)
            return Result.Failure<ClientPackageRefreshResult>("В релизе нет APK приложения");

        var folder = ClientPackageCatalog.ApplicationFolder(application);
        if (folder is null)
            return Result.Failure<ClientPackageRefreshResult>("Некорректное имя приложения");

        Directory.CreateDirectory(folder);

        foreach (var path in Directory.EnumerateFiles(folder, "*.incoming"))
            File.Delete(path);

        foreach (var path in Directory.EnumerateFiles(folder, "*.part"))
            File.Delete(path);

        var downloaded = 0;
        var errors = new List<string>();

        foreach (var asset in selected)
        {
            if (!TryAbi(asset.Name, application, out var abi))
                continue;

            var existing = ClientPackageCatalog.Latest(application, abi);
            if (existing is not null && ReleaseDigest.Matches(asset.Digest, existing.Sha256))
                continue;

            var incomingPath = Path.Combine(folder, Path.GetFileName(asset.Name) + ".incoming");
            var downloadResult = await _releaseClient
                .DownloadTo(asset, incomingPath, ClientPackageStore.MaxPackageBytes, cancellationToken)
                .ConfigureAwait(false);

            if (downloadResult.IsFailure)
            {
                _logger.LogError(
                    "Не удалось скачать пакет {AssetName}: {Error}",
                    asset.Name,
                    downloadResult.Error);
                errors.Add($"{asset.Name}: {downloadResult.Error}");
                continue;
            }

            if (!TryPackage(downloadResult.Value, asset.Name, application, abi, out var package))
            {
                File.Delete(downloadResult.Value);
                errors.Add($"{asset.Name}: не удалось прочитать версию APK");
                continue;
            }

            var sha256 = await ComputeSha256(downloadResult.Value, cancellationToken).ConfigureAwait(false);
            if (!ReleaseDigest.Matches(asset.Digest, sha256))
            {
                File.Delete(downloadResult.Value);
                errors.Add($"{asset.Name}: контрольная сумма не совпала с digest релиза");
                continue;
            }

            if (existing is not null && existing.Package.VersionCode >= package.VersionCode)
            {
                File.Delete(downloadResult.Value);
                continue;
            }

            var destinationPath = Path.Combine(folder, package.FileName);
            if (!string.Equals(downloadResult.Value, destinationPath, StringComparison.OrdinalIgnoreCase))
                File.Move(downloadResult.Value, destinationPath, overwrite: true);

            await File.WriteAllTextAsync(destinationPath + ".sha256", sha256, cancellationToken)
                .ConfigureAwait(false);

            ClientPackageStore.DeleteOthers(folder, package, destinationPath);
            downloaded++;

            _logger.LogInformation(
                "Пакет {FileName} версии {VersionName} сборки {VersionCode} сохранён",
                package.FileName,
                package.VersionName,
                package.VersionCode);
        }

        return Result.Success(new ClientPackageRefreshResult(downloaded, errors));
    }

    private static bool TryAbi(string fileName, string application, out string abi)
    {
        if (ClientPackageName.TryParse(fileName, out var package)
            && string.Equals(package.Application, application, StringComparison.OrdinalIgnoreCase))
        {
            abi = package.Abi;
            return true;
        }

        return ClientPackageName.TryParseFlutterApk(fileName, out abi);
    }

    private static bool TryPackage(
        string apkPath,
        string assetName,
        string application,
        string abi,
        out ClientPackage package)
    {
        if (ApkVersion.TryRead(apkPath, out var versionName, out var versionCode))
        {
            package = new ClientPackage(application, versionName, versionCode, abi);
            return true;
        }

        if (ClientPackageName.TryParse(assetName, out package) && package.VersionCode > 0)
        {
            package = new ClientPackage(application, package.VersionName, package.VersionCode, abi);
            return true;
        }

        package = default;
        return false;
    }

    private async Task<string?> SaveUpload(
        string folder,
        string application,
        ClientPackageUpload upload,
        CancellationToken cancellationToken)
    {
        var safeName = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "apk";

        var incoming = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".incoming");
        try
        {
            var copyError = await CopyLimited(upload.Content, incoming, upload.Length, cancellationToken)
                .ConfigureAwait(false);
            if (copyError is not null)
                return $"{safeName}: {copyError}";

            var stored = ClientPackageStore.Accept(folder, incoming, safeName, application);
            if (stored.IsFailure)
                return $"{safeName}: {stored.Error}";

            _logger.LogInformation(
                "Пакет {FileName} версии {VersionName} сборки {VersionCode} сохранён с диска",
                stored.Value.FileName,
                stored.Value.VersionName,
                stored.Value.VersionCode);
            return null;
        }
        finally
        {
            if (File.Exists(incoming))
                File.Delete(incoming);
        }
    }

    private static async Task<string?> CopyLimited(
        Stream source,
        string destination,
        long declaredLength,
        CancellationToken cancellationToken)
    {
        if (declaredLength <= 0)
            return "Файл APK пустой";

        if (declaredLength > ClientPackageStore.MaxPackageBytes)
            return "APK больше 80 МБ";

        await using (var output = File.Create(destination))
        {
            var buffer = new byte[81920];
            long total = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;

                total += read;
                if (total > ClientPackageStore.MaxPackageBytes)
                    return "APK больше 80 МБ";

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            if (total == 0)
                return "Файл APK пустой";
        }

        return null;
    }

    private static async Task<string> ComputeSha256(string filePath, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(fileStream, cancellationToken).ConfigureAwait(false);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
