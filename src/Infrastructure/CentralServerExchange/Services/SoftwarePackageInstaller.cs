using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Constants;
using FmuApiDomain.Files;
using FmuApiDomain.SoftwareUpdate.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;

namespace CentralServerExchange.Services;

/// Установка архива обновления: распаковка и запуск установщика.
[AutoRegisterService(ServiceLifetime.Singleton)]
public class SoftwarePackageInstaller : ISoftwarePackageInstaller
{
    private const string HostExeName = "fmu-api.exe";
    private static readonly TimeSpan LinuxInstallTimeout = TimeSpan.FromMinutes(15);

    /// Установщик один на процесс: установку берут и центр, и GitHub.
    private static readonly SemaphoreSlim InstallLock = new(1, 1);

    private readonly ILogger<SoftwarePackageInstaller> _logger;

    public SoftwarePackageInstaller(ILogger<SoftwarePackageInstaller> logger)
    {
        _logger = logger;
    }

    public async Task<Result> InstallAsync(string zipPath, string sha256)
    {
        if (!await InstallLock.WaitAsync(0).ConfigureAwait(false))
            return Result.Failure("Установка обновления уже запущена");

        try
        {
            if (OperatingSystem.IsWindows())
                return UpdateWindowsApp(zipPath, sha256);

            if (OperatingSystem.IsLinux())
                return await UpdateLinuxApp(zipPath, sha256).ConfigureAwait(false);

            return Result.Failure("Не поддерживаемая ОС");
        }
        finally
        {
            InstallLock.Release();
        }
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

    /// Отметка об установленном пакете в каталоге данных службы.
    private void WriteChecksum(string sha256)
    {
        var dataFolder = ServiceFolders.DataFolder();

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

    private async Task<Result> UpdateLinuxApp(string updateFileName, string sha256)
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
            KillProcess(process);
            return Result.Failure($"Таймаут установки обновления ({LinuxInstallTimeout.TotalMinutes} мин)");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка запуска установщика: {ex.Message}");
        }

        if (process.ExitCode != 0)
            return Result.Failure($"Установщик завершился с кодом {process.ExitCode}");

        // Установщик Linux --checksum не разбирает: отметку об установленном пакете пишем сами.
        WriteChecksum(sha256);

        _logger.LogInformation("Установка обновления на Linux завершена успешно");
        return Result.Success();
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
        }
    }

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
