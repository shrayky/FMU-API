using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Constants;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.FilesFolders;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace AutoUpdateWorkerService.Workers;

public class AutoUpdateWorker : BackgroundService
{
    private readonly IParametersService _parametersService;
    private readonly ILogger<AutoUpdateWorker> _logger;

    public AutoUpdateWorker(IParametersService parametersService, ILogger<AutoUpdateWorker> logger)
    {
        _parametersService = parametersService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var configuration = await _parametersService.CurrentAsync();
            var checkIntervalMinutes = configuration.AutoUpdate.CheckUpdateIntervalMinutes;

            await Task.Delay(TimeSpan.FromMinutes(checkIntervalMinutes), stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            if (!configuration.AutoUpdate.Enabled) 
                continue;
            
            _logger.LogInformation("Проверяю наличие обновления в каталоге {UpdateFilesCatalog}",
                configuration.AutoUpdate.UpdateFilesCatalog);

            _ = CheckUpdates(configuration.AutoUpdate);
        }
    }

    private Result CheckUpdates(AutoUpdateOptions options)
    {
        if (!options.Enabled || !(options.FromHour <= DateTime.Now.Hour && options.CanUpdateUntil() > DateTime.Now.Hour))
            return Result.Success();

        var architecture = Environment.Is64BitProcess ? "x64" : "x86";
        var os = OperatingSystem.IsWindows() ? "win" : "linux";
        var updateFileName = Path.Combine(options.UpdateFilesCatalog, $"update_{architecture}_{os}.zip");

        if (!File.Exists(updateFileName))
        {
            _logger.LogInformation("Не найден файл обновления {updateFileName}",  updateFileName);
            return Result.Failure("No updates");
        }

        var updateFileChecksum = GetFileMd5Checksum(updateFileName);
        var currentInstanceChecksum = GetCurrentInstanceChecksum();

        _logger.LogInformation("Обновление с {currentInstanceChecksum} на {updateFileChecksum}", currentInstanceChecksum, updateFileChecksum);

        if (currentInstanceChecksum == updateFileChecksum)
            return Result.Success();

        if (OperatingSystem.IsWindows())
            UpdateWindowsApp(updateFileName, updateFileChecksum);
        else
            _logger.LogWarning("Автообновление не поддерживается данной ОС.");

        return Result.Success();
    }

    private void UpdateWindowsApp(string updateFileName, string updateFileChecksum)
    {
        _logger.LogWarning("Найдено обновление, запускаю установку.");

        var installerPath = Path.Combine(Path.GetTempPath(), ApplicationInformation.AppName);

        if (Directory.Exists(installerPath))
            Directory.Delete(installerPath, true);

        Directory.CreateDirectory(installerPath);

        ZipFile.ExtractToDirectory(updateFileName, installerPath);

        var installerExe = Path.Combine(installerPath, $"{ApplicationInformation.AppName}.exe");
        if (!File.Exists(installerExe))
        {
            _logger.LogError("В архиве обновления не найден исполняемый файл: {InstallerExe}", installerExe);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = installerExe,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("--install");
        startInfo.ArgumentList.Add("--checksum");
        startInfo.ArgumentList.Add(updateFileChecksum);
        startInfo.ArgumentList.Add("--waitForPid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString());

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            _logger.LogError("Не удалось запустить процесс установщика.");
            return;
        }

        _logger.LogInformation(
            "Установщик запущен (PID={InstallerPid}). Завершаю текущий процесс службы (PID={ServicePid}) для освобождения файлов.",
            process.Id,
            Environment.ProcessId);

        Thread.Sleep(500);
        Environment.Exit(0);
    }

    private string GetFileMd5Checksum(string filePath)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при вычислении MD5 для файла: {FilePath}", filePath);
            return string.Empty;
        }
    }

    private string GetCurrentInstanceChecksum()
    {
        var dataFolder = Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName);
        var checkSumFileName = Path.Combine(dataFolder, "checksum.txt");

        return !File.Exists(checkSumFileName) ? string.Empty : File.ReadAllText(checkSumFileName);
    }

}
