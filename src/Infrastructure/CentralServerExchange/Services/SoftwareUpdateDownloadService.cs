using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Constants;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class SoftwareUpdateDownloadService
{
    private const string HostExeName = "fmu-api.exe";
    private const int DownloadRetryCount = 3;
    private static readonly TimeSpan LinuxInstallTimeout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DownloadRetryDelay = TimeSpan.FromSeconds(5);

    private readonly ILogger<SoftwareUpdateDownloadService> _logger;
    private readonly IParametersService _parametersService;
    private readonly IExchangeService _exchangeService;
    private readonly IApplicationState _appState;

    private static readonly SemaphoreSlim UpdateLock = new(1, 1);

    public SoftwareUpdateDownloadService(
        ILogger<SoftwareUpdateDownloadService> logger,
        IParametersService parametersService,
        IExchangeService exchangeService,
        IApplicationState appState)
    {
        _logger = logger;
        _parametersService = parametersService;
        _exchangeService = exchangeService;
        _appState = appState;
    }

    public async Task<Result> DownloadAndInstall(FmuApiCentralResponse response, string baseAddress)
    {
        var parameters = await _parametersService.CurrentAsync();

        if (!parameters.FmuApiCentralServer.DownloadNewVersion
            || !_appState.IsOnline()
            || !response.SoftwareUpdateAvailable)
            return Result.Success();

        if (parameters.FmuApiCentralServer.SchedulerUpdateInstall.Count > 0)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            var isInSchedule = parameters.FmuApiCentralServer.SchedulerUpdateInstall
                .Any(interval => IsWithinSchedule(now, interval));

            if (!isInSchedule)
            {
                _logger.LogInformation("Обновление отложено: текущее время вне разрешённых интервалов");
                return Result.Success();
            }
        }

        if (!await UpdateLock.WaitAsync(0))
            return Result.Failure("Обновление уже запущено");

        try
        {
            _logger.LogInformation("Доступно обновление ПО в центральном сервере");

            var token = parameters.FmuApiCentralServer.Token;
            var sha256 = response.UpdateHash;

            if (string.IsNullOrWhiteSpace(sha256))
                return Result.Failure("Пустой UpdateHash для доступного обновления");

            if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
                return Result.Failure($"Некорректный формат UpdateHash: {sha256}");

            var requestAddress = $"{baseAddress}/fmuApiUpdate/{token}";

            var downloadResult = await DownloadAndVerifyAsync(requestAddress, sha256).ConfigureAwait(false);

            if (downloadResult.IsFailure)
            {
                _logger.LogError(downloadResult.Error);
                return Result.Failure(downloadResult.Error);
            }

            var installResult = await InstallUpdate(downloadResult.Value, sha256).ConfigureAwait(false);

            if (installResult.IsSuccess)
                return Result.Success();

            _logger.LogError(installResult.Error);
            return Result.Failure(installResult.Error);
        }
        finally
        {
            UpdateLock.Release();
        }
    }

    /// <summary>
    /// Скачивает обновление с повторами и проверяет SHA-256. При несовпадении хэша частичный файл удаляется.
    /// </summary>
    private async Task<Result<string>> DownloadAndVerifyAsync(string requestAddress, string sha256)
    {
        var lastError = "Загрузка обновления не выполнялась";

        for (var attempt = 1; attempt <= DownloadRetryCount; attempt++)
        {
            var downloadResult = await _exchangeService
                .DownloadSoftwareUpdateToTemp(requestAddress, sha256)
                .ConfigureAwait(false);

            if (downloadResult.IsFailure)
            {
                lastError = downloadResult.Error;
                LogDownloadAttempt(attempt, lastError);

                if (attempt < DownloadRetryCount)
                    await Task.Delay(DownloadRetryDelay).ConfigureAwait(false);

                continue;
            }

            var fileName = downloadResult.Value;
            var checkResult = await CheckShaHash(fileName, sha256).ConfigureAwait(false);

            if (checkResult.IsSuccess)
                return Result.Success(PromotePartialToZip(fileName));

            TryDeleteFile(fileName);
            lastError = checkResult.Error;
            LogDownloadAttempt(attempt, lastError);

            if (attempt < DownloadRetryCount)
                await Task.Delay(DownloadRetryDelay).ConfigureAwait(false);
        }

        return Result.Failure<string>(lastError);
    }

    private void LogDownloadAttempt(int attempt, string error)
    {
        _logger.LogWarning(
            "Попытка {Attempt}/{Max} загрузки обновления не удалась: {Error}",
            attempt,
            DownloadRetryCount,
            error);
    }

    /// <summary>
    /// Переименовывает докачанный .partial в .zip перед установкой.
    /// </summary>
    private static string PromotePartialToZip(string fileName)
    {
        if (!fileName.EndsWith(".partial", StringComparison.OrdinalIgnoreCase))
            return fileName;

        var zipPath = Path.ChangeExtension(fileName, ".zip");
        File.Move(fileName, zipPath, overwrite: true);
        return zipPath;
    }

    /// <summary>
    /// Проверяет попадание текущего времени в интервал, включая окна через полночь.
    /// </summary>
    private static bool IsWithinSchedule(TimeOnly now, ScheduleTime interval)
    {
        if (interval.BeginTime <= interval.EndTime)
            return now >= interval.BeginTime && now <= interval.EndTime;

        return now >= interval.BeginTime || now <= interval.EndTime;
    }

    private async Task<Result> CheckShaHash(string filePath, string expectedSha256)
    {
        await using var fileStream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(fileStream).ConfigureAwait(false);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        if (string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        var errorMessage = $"Хэш {actualHash} загруженного файла обновления не совпадает с ожидаемым {expectedSha256}";
        _logger.LogError(errorMessage);

        return Result.Failure(errorMessage);
    }

    private async Task<Result> InstallUpdate(string updateFileName, string sha256)
    {
        if (OperatingSystem.IsWindows())
            return UpdateWindowsApp(updateFileName, sha256);

        if (OperatingSystem.IsLinux())
            return await UpdateLinuxApp(updateFileName).ConfigureAwait(false);

        return Result.Failure("Не поддерживаемая ОС");
    }

    private Result UpdateWindowsApp(string updateFileName, string sha256)
    {
        var stagingPath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName, "updates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingPath);

        var validateResult = ValidateZipEntries(updateFileName, stagingPath);
        if (validateResult.IsFailure)
        {
            TryDeleteDirectory(stagingPath);
            return validateResult;
        }

        try
        {
            ZipFile.ExtractToDirectory(updateFileName, stagingPath, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось распаковать обновление");
            TryDeleteDirectory(stagingPath);
            return Result.Failure(ex.Message);
        }

        TryDeleteFile(updateFileName);

        var hostExe = Path.Combine(stagingPath, HostExeName);

        // Host в пакете: --install. Staging не удаляем — из него работает установщик.
        // Текущий процесс завершаем, чтобы сработал --waitForPid.
        if (File.Exists(hostExe))
        {
            _logger.LogInformation("В пакете найден {Host} — запускаю --install", HostExeName);
            return RunHostInstallAndExit(hostExe, sha256);
        }

        _logger.LogInformation("Host в пакете нет — копирую версии продуктов без переустановки службы");

        try
        {
            var applyResult = CopyProductVersionsFromStaging(stagingPath);
            if (applyResult.IsSuccess)
                WriteChecksum(sha256);

            return applyResult;
        }
        finally
        {
            TryDeleteDirectory(stagingPath);
        }
    }

    /// <summary>
    /// Запускает host --install и завершает текущий процесс (для --waitForPid).
    /// </summary>
    private Result RunHostInstallAndExit(string hostExePath, string sha256)
    {
        var startInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = hostExePath,
            WorkingDirectory = Path.GetDirectoryName(hostExePath) ?? AppContext.BaseDirectory,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("--install");
        startInfo.ArgumentList.Add("--checksum");
        startInfo.ArgumentList.Add(sha256);
        startInfo.ArgumentList.Add("--waitForPid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());

        _logger.LogWarning("Запускаю установку host: {File} --install --checksum ...", hostExePath);

        using var process = Process.Start(startInfo);
        if (process is null)
            return Result.Failure("Не удалось запустить host --install");

        _logger.LogInformation(
            "Установщик запущен (PID={InstallerPid}). Завершаю текущий процесс (PID={Pid}) для --waitForPid.",
            process.Id,
            Environment.ProcessId);

        Thread.Sleep(500);
        Environment.Exit(0);

        return Result.Success();
    }

    /// <summary>
    /// Копирует из staging каталоги вида {product}\{ver}\{product}.exe в каталог установки.
    /// Host сам подхватит старшую версию при следующем скане.
    /// </summary>
    private Result CopyProductVersionsFromStaging(string stagingPath)
    {
        var installRoot = GetInstallDirectory();
        if (!Directory.Exists(installRoot))
            return Result.Failure($"Каталог установки не найден: {installRoot}");

        var copied = 0;

        foreach (var productDir in Directory.EnumerateDirectories(stagingPath))
        {
            var productName = Path.GetFileName(productDir);
            if (string.IsNullOrWhiteSpace(productName))
                continue;

            foreach (var versionDir in Directory.EnumerateDirectories(productDir))
            {
                var versionName = Path.GetFileName(versionDir);
                if (!Version.TryParse(versionName, out var version))
                {
                    _logger.LogDebug("Пропуск {Dir}: имя не является версией", versionDir);
                    continue;
                }

                var expectedExe = Path.Combine(versionDir, $"{productName}.exe");
                if (!File.Exists(expectedExe))
                {
                    _logger.LogWarning(
                        "Пропуск {Product} {Version}: нет файла {Exe}",
                        productName,
                        versionName,
                        $"{productName}.exe");
                    continue;
                }

                var versionFolder = $"{version.Major}.{version.Minor}";
                var targetDir = Path.Combine(installRoot, productName, versionFolder);
                var partialDir = targetDir + ".partial";

                try
                {
                    if (Directory.Exists(partialDir))
                        Directory.Delete(partialDir, true);

                    CopyDirectory(versionDir, partialDir);

                    if (Directory.Exists(targetDir))
                        Directory.Delete(targetDir, true);

                    Directory.Move(partialDir, targetDir);
                    copied++;

                    _logger.LogInformation(
                        "Установлена версия продукта {Product} {Version} → {Target}",
                        productName,
                        versionFolder,
                        targetDir);
                }
                catch (Exception ex)
                {
                    TryDeleteDirectory(partialDir);
                    return Result.Failure($"Ошибка копирования {productName} {versionFolder}: {ex.Message}");
                }
            }
        }

        if (copied == 0)
            return Result.Failure("В пакете обновления не найдено ни одной версии продукта для копирования");

        return Result.Success();
    }

    private void WriteChecksum(string sha256)
    {
        var dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            ApplicationInformation.Manufacture,
            ApplicationInformation.AppName);

        Directory.CreateDirectory(dataFolder);
        File.WriteAllText(Path.Combine(dataFolder, "checksum.txt"), sha256);
        _logger.LogInformation("Записан checksum обновления");
    }

    private static string GetInstallDirectory() =>
        Path.Combine(
            Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\",
            "Program Files",
            ApplicationInformation.Manufacture,
            ApplicationInformation.AppName);

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);

        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
    }

    /// <summary>
    /// Устанавливает обновление на Linux и ждёт завершения установщика.
    /// </summary>
    private async Task<Result> UpdateLinuxApp(string updateFileName)
    {
        _logger.LogWarning("Начинаю установку обновления");

        var installerPath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);

        var validateResult = ValidateZipEntries(updateFileName, installerPath);
        if (validateResult.IsFailure)
            return validateResult;

        try
        {
            Directory.CreateDirectory(installerPath);
            ZipFile.ExtractToDirectory(updateFileName, installerPath, true);
            TryDeleteFile(updateFileName);
        }
        catch (Exception e)
        {
            return Result.Failure($"Ошибка распаковки обновления в {installerPath}: {e.Message}");
        }

        var installerFile = Path.Combine(
            Path.GetTempPath(),
            ApplicationInformation.AppName,
            ApplicationInformation.AppName.ToLowerInvariant());

        if (!File.Exists(installerFile))
            return Result.Failure($"Файл установщика не найден: {installerFile}");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            FileName = installerFile,
            CreateNoWindow = true,
            Arguments = "--install",
            RedirectStandardOutput = true,
        };

        try
        {
            if (!process.Start())
                return Result.Failure("Не удалось запустить установщик обновления");

            using var timeoutCts = new CancellationTokenSource(LinuxInstallTimeout);
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            return Result.Failure($"Таймаут установки обновления ({LinuxInstallTimeout.TotalMinutes} мин)");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка запуска установщика: {ex.Message}");
        }

        if (process.ExitCode != 0)
            return Result.Failure($"Установщик завершился с кодом {process.ExitCode}");

        _logger.LogInformation("Установка обновления на Linux завершена успешно");
        return Result.Success();
    }

    /// <summary>
    /// Проверяет zip на path traversal (zip-slip) перед распаковкой.
    /// </summary>
    private static Result ValidateZipEntries(string zipPath, string destinationDir)
    {
        try
        {
            var fullDest = Path.GetFullPath(destinationDir);
            if (!fullDest.EndsWith(Path.DirectorySeparatorChar)
                && !fullDest.EndsWith(Path.AltDirectorySeparatorChar))
            {
                fullDest += Path.DirectorySeparatorChar;
            }

            using var archive = ZipFile.OpenRead(zipPath);

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.FullName))
                    continue;

                var destinationPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));
                if (!destinationPath.StartsWith(fullDest, StringComparison.OrdinalIgnoreCase))
                    return Result.Failure($"Небезопасный путь в архиве обновления: {entry.FullName}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Не удалось проверить архив обновления: {ex.Message}");
        }
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch
        {
            _logger.LogWarning("Не удалось удалить временные файлы: {Path}", path);
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
