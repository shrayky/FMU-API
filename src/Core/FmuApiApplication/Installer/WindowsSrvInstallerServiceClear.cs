using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Constants;
using Shared.FilesFolders;
using Shared.Strings;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.ServiceProcess;
using System.Text.RegularExpressions;

namespace FmuApiApplication.Installer;

[SupportedOSPlatform("windows")]
public class WindowsSrvInstallerServiceClear
{
    private readonly IParametersService _parametersService;

    private readonly Parameters _configuration;
    private readonly string _serviceName = ApplicationInformation.AppName.ToLower();
    private readonly string _serviceDisplayName = ApplicationInformation.ServiceName;
    private readonly string _installDirectory;
    private  string _logDirectory;
    private string? _logFilePath;
    private readonly string _exeName = string.Empty;
    private readonly string _wwwroot = "wwwroot";

    public WindowsSrvInstallerServiceClear(IParametersService parametersService)
    {
        _parametersService = parametersService;
        
        _installDirectory = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "",
                                        "Program Files",
                                        ApplicationInformation.Manufacture,
                                        ApplicationInformation.AppName);
        
        _configuration = _parametersService.Current();
        _logDirectory = Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName);

        _exeName = string.Concat(ApplicationInformation.AppName.ToLower(), ".exe");
    }

    private void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    public async Task<bool> InstallAsync(string[] installerArgs)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        StartOperationLog("installer");

        ServiceController? existingService = null;
        var serviceExistedBeforeInstall = false;
        var serviceWasRunningBeforeInstall = false;

        var bin = Path.Combine(_installDirectory, _exeName);
        var wwwroot = Path.Combine(_installDirectory, _wwwroot);
        var backupRoot = Path.Combine(_installDirectory, "_install_backup");

        try
        {
            LogInfo("Старт установки сервиса.");
            LogInstallerDiagnostics();

            LogInfo($"Аргументы установки: {string.Join(" ", installerArgs.Select(a => $"\"{a}\""))}");

            using (var serviceSnapshot = GetExistingService())
            {
                if (serviceSnapshot is not null)
                {
                    serviceSnapshot.Refresh();
                    serviceExistedBeforeInstall = true;
                    serviceWasRunningBeforeInstall = serviceSnapshot.Status == ServiceControllerStatus.Running;
                    LogInfo(
                        $"Снимок службы до ожидания PID: зарегистрирована, статус={serviceSnapshot.Status}, нужен запуск после rollback={serviceWasRunningBeforeInstall}.");
                }
            }

            WaitForSourceProcessExit(installerArgs);

            LogInfo("После ожидания завершения службы.");
            LogInstallerDiagnostics();

            EnsureInstallDirectory();

            existingService = GetExistingService();

            if (existingService is not null)
            {
                LogInfo("Остановка существующего сервиса.");
                StopService(existingService);
                KillResidualAppProcesses();
                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            BackupCurrentInstallation(bin, wwwroot, backupRoot);
            LogInfo("Backup текущей установки выполнен.");

            if (!await DeleteInstallationFilesAsync(bin, wwwroot))
                throw new IOException($"Не удалось удалить файлы установки: {bin}");

            LogInfo("Старые файлы установки удалены.");

            var setupFolder = GetSetupFolder();
            CopyFilesRecursively(setupFolder, _installDirectory);
            LogInfo("Новые файлы скопированы.");

            if (!serviceExistedBeforeInstall)
            {
                LogInfo("Регистрация нового Windows-сервиса.");
                await CreateAndConfigureServiceAsync(bin);
            }

            await UpdateInstallParametersAsync(installerArgs);
            WriteChecksum(installerArgs);

            using var targetService = GetExistingService();
            if (targetService is null)
                throw new InvalidOperationException($"Не найден сервис '{_serviceName}' после установки.");

            StartService(targetService);
            LogInfo("Сервис успешно запущен.");

            DeleteBackup(backupRoot);
            LogInfo("Установка завершена успешно.");

            return true;
        }
        catch (Exception ex)
        {
            LogError($"Ошибка установки: {ex.Message}");
            var rollbackResult = await RollbackInstallAsync(bin, wwwroot, backupRoot, serviceExistedBeforeInstall, serviceWasRunningBeforeInstall);
            LogInfo(rollbackResult
                ? "Выполнен rollback к исходному состоянию."
                : "Rollback завершился с ошибкой.");
            return false;
        }
        finally
        {
            existingService?.Dispose();
        }
    }

    private async Task CreateAndConfigureServiceAsync(string binPath)
    {
        var createCommand = $"sc create {_serviceName} binPath= \"{binPath} --service\" DisplayName= \"{_serviceDisplayName}\" type= own start= auto";
        var failureCommand = $"sc failure \"{_serviceName}\" reset= 5 actions= restart/5000";
        var deleteRuleCommand = $"netsh advfirewall firewall delete rule name = \"{_serviceName}\"";
        var addRuleCommand = $"netsh advfirewall firewall add rule name = \"{_serviceName}\" dir =in action = allow protocol = TCP localport = 2578";

        await ExecuteCommandAsync(createCommand, "Создание сервиса");
        await ExecuteCommandAsync(failureCommand, "Настройка перезапуска сервиса");
        await ExecuteCommandAsync(deleteRuleCommand, "Удаление старого правила firewall");
        await ExecuteCommandAsync(addRuleCommand, "Добавление правила firewall");
    }

    private async Task ExecuteCommandAsync(string command, string operationName)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var stdErr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(stdOut))
            LogInfo($"{operationName}: {stdOut.Trim()}");

        if (!string.IsNullOrWhiteSpace(stdErr))
            LogError($"{operationName}: {stdErr.Trim()}");

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{operationName} завершилось с кодом {process.ExitCode}. Команда: {command}");
        }
    }

    private void StopService(ServiceController service)
    {
        service.Refresh();
        if (service.Status == ServiceControllerStatus.Stopped)
            return;

        // На попытку даём до 30 с: у .NET Generic Host и Kestrel остановка часто дольше 10 с; при дочернем установщике SCM может долго держать StopPending.
        var waitPerAttempt = TimeSpan.FromSeconds(30);
        const int stopAttempts = 3;
        for (var attempt = 1; attempt <= stopAttempts; attempt++)
        {
            service.Refresh();
            if (service.Status == ServiceControllerStatus.Stopped)
                return;

            if (service.Status is ServiceControllerStatus.Running or ServiceControllerStatus.Paused)
            {
                LogInfo($"Попытка {attempt}/{stopAttempts}: штатная остановка сервиса '{service.ServiceName}' (ServiceController.Stop → SCM).");
                service.Stop();
            }

            if (TryWaitForServiceStatusWithProgress(service, ServiceControllerStatus.Stopped, waitPerAttempt,
                    $"Ожидание остановки, попытка {attempt}/{stopAttempts}"))
                return;
        }

        LogError($"Штатная остановка сервиса '{service.ServiceName}' не выполнена. Пытаюсь завершить процесс принудительно.");
        var pid = TryGetServiceProcessId(service.ServiceName);
        if (pid is null || pid <= 0)
            throw new InvalidOperationException($"Не удалось определить PID сервиса '{service.ServiceName}' для принудительной остановки.");

        ForceKillServiceProcess(pid.Value);

        if (!TryWaitForServiceStatus(service, ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15)))
            throw new System.TimeoutException($"Сервис '{service.ServiceName}' не остановился после taskkill.");
    }

    private void StartService(ServiceController service)
    {
        service.Refresh();
        if (service.Status == ServiceControllerStatus.Running)
            return;

        service.Start();
        WaitForServiceStatus(service, ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
    }

    private void WaitForServiceStatus(ServiceController service, ServiceControllerStatus targetStatus, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt <= timeout)
        {
            service.Refresh();
            if (service.Status == targetStatus)
                return;

            Thread.Sleep(500);
        }

        throw new System.TimeoutException($"Сервис '{service.ServiceName}' не перешел в состояние '{targetStatus}'.");
    }

    private bool TryWaitForServiceStatus(ServiceController service, ServiceControllerStatus targetStatus, TimeSpan timeout)
    {
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt <= timeout)
        {
            service.Refresh();
            if (service.Status == targetStatus)
                return true;

            Thread.Sleep(500);
        }

        return false;
    }

    private bool TryWaitForServiceStatusWithProgress(
        ServiceController service,
        ServiceControllerStatus targetStatus,
        TimeSpan timeout,
        string progressLabel)
    {
        var startedAt = DateTime.UtcNow;
        var lastLog = startedAt;
        while (DateTime.UtcNow - startedAt <= timeout)
        {
            service.Refresh();
            if (service.Status == targetStatus)
                return true;

            if (DateTime.UtcNow - lastLog >= TimeSpan.FromSeconds(5))
            {
                LogInfo($"{progressLabel}: статус службы '{service.ServiceName}' = {service.Status}");
                lastLog = DateTime.UtcNow;
            }

            Thread.Sleep(500);
        }

        service.Refresh();
        LogInfo($"{progressLabel}: таймаут, последний статус = {service.Status}");
        return false;
    }

    private int? TryGetServiceProcessId(string serviceName)
    {
        var (exitCode, stdOut, stdErr) = ExecuteCommandWithResult($"sc queryex \"{serviceName}\"");

        if (!string.IsNullOrWhiteSpace(stdOut))
            LogInfo($"Получение PID сервиса: {stdOut.Trim()}");
        if (!string.IsNullOrWhiteSpace(stdErr))
            LogError($"Ошибка получения PID сервиса: {stdErr.Trim()}");

        if (exitCode != 0)
            return null;

        var match = Regex.Match(stdOut, @"PID\s*:\s*(\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        return int.Parse(match.Groups[1].Value);
    }

    private void ForceKillServiceProcess(int pid)
    {
        // Без /T: установщик из temp часто дочерний процесс службы — дерево нельзя рубить, иначе "Cannot be used to terminate a process tree containing the calling process".
        var (exitCode, stdOut, stdErr) = ExecuteCommandWithResult($"taskkill /PID {pid} /F");

        if (!string.IsNullOrWhiteSpace(stdOut))
            LogInfo($"Принудительная остановка PID {pid}: {stdOut.Trim()}");
        if (!string.IsNullOrWhiteSpace(stdErr))
            LogError($"Ошибка принудительной остановки PID {pid}: {stdErr.Trim()}");

        if (exitCode != 0)
            throw new InvalidOperationException($"taskkill завершился с кодом {exitCode} для PID {pid}.");
    }

    private (int ExitCode, string StdOut, string StdErr) ExecuteCommandWithResult(string command)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdOut, stdErr);
    }

    public bool Uninstall()
    {
        Unregister();

        var bin = Path.Combine(_installDirectory, _exeName);
        var wwwroot = Path.Combine(_installDirectory, _wwwroot);

        if (File.Exists(bin))
            File.Delete(bin);

        if (Directory.Exists(wwwroot))
            Directory.Delete(wwwroot, true);

        return true;
    }

    public bool RegisterWindowsService(string[] args)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        StartOperationLog("register");

        var existingService = GetExistingService();
        var bin = Path.Combine(_installDirectory, _exeName);

        if (existingService != null)
        {
            StopService(existingService);
        }
        else
        {
            var create = ExecuteCommandAsync($"sc create {_serviceName} binPath= \"{bin} --service\" DisplayName= \"{_serviceDisplayName}\" type= own start= auto", "Создание сервиса");
            create.GetAwaiter().GetResult();
            ExecuteCommandAsync($"sc failure \"{_serviceName}\" reset= 5 actions= restart/5000", "Настройка перезапуска сервиса").GetAwaiter().GetResult();
            ExecuteCommandAsync($"netsh advfirewall firewall delete rule name = \"{_serviceName}\"", "Удаление старого правила firewall").GetAwaiter().GetResult();
            ExecuteCommandAsync($"netsh advfirewall firewall add rule name = \"{_serviceName}\" dir =in action = allow protocol = TCP localport = 2578", "Добавление правила firewall").GetAwaiter().GetResult();
        }

        using var targetService = GetExistingService();
        if (targetService is null)
            return false;

        StartService(targetService);

        existingService?.Dispose();

        return true;
    }

    public bool Unregister()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        StartOperationLog("unregister");

        var existingService = GetExistingService();

        if (existingService is null)
            return true;

        StopService(existingService);

        ExecuteCommandAsync($"sc delete {_serviceName}", "Удаление сервиса").GetAwaiter().GetResult();

        existingService?.Dispose();

        return true;
    }

    private ServiceController? GetExistingService()
    {
        return ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == _serviceName);
    }

    private void EnsureInstallDirectory()
    {
        if (!Directory.Exists(_installDirectory))
            Directory.CreateDirectory(_installDirectory);
    }

    private string GetSetupFolder()
    {
        var serviceFileName = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
        return Path.GetDirectoryName(serviceFileName) ?? serviceFileName.Replace(_exeName, "");
    }

    private void LogInstallerDiagnostics()
    {
        LogInfo($"Текущий процесс установки: PID={Environment.ProcessId}, путь={Environment.ProcessPath ?? "(неизвестно)"}");

        var processes = Process.GetProcessesByName(_serviceName);
        LogInfo($"Процессы с именем '{_serviceName}' (образ {_exeName}): найдено {processes.Length}.");

        foreach (var p in processes)
        {
            try
            {
                var imagePath = p.MainModule?.FileName ?? "(нет)";
                LogInfo($"  PID={p.Id}, путь к образу={imagePath}");
            }
            catch (Exception ex)
            {
                LogInfo($"  PID={p.Id}, путь к образу недоступен: {ex.Message}");
            }
            finally
            {
                p.Dispose();
            }
        }
    }

    private void WaitForSourceProcessExit(string[] installerArgs)
    {
        if (!TryParseWaitForPid(installerArgs, out var waitForPid))
        {
            LogInfo("Параметр --waitForPid не задан или не распознан (ожидание процесса-источника пропущено).");
            return;
        }

        if (waitForPid <= 0 || waitForPid == Environment.ProcessId)
        {
            LogInfo($"waitForPid={waitForPid} пропущен (некорректный или совпадает с текущим процессом).");
            return;
        }

        LogInfo($"Ожидаю завершения процесса-источника обновления PID={waitForPid}.");

        Process? sourceProcess = null;
        try
        {
            sourceProcess = Process.GetProcessById(waitForPid);
        }
        catch (ArgumentException)
        {
            LogInfo($"Процесс PID={waitForPid} уже завершён до ожидания.");
            return;
        }

        using (sourceProcess)
        {
            if (!sourceProcess.WaitForExit(120000))
                throw new System.TimeoutException($"Процесс-источник обновления PID={waitForPid} не завершился за 120 секунд.");
        }

        LogInfo($"Процесс-источник PID={waitForPid} завершился.");
    }

    private static bool TryParseWaitForPid(string[] args, out int pid)
    {
        pid = 0;

        var fromHelper = StringHelpers.ArgumentValue(args, "--waitForPid", "");
        if (int.TryParse(fromHelper, out pid) && pid > 0)
            return true;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i].Trim();
            if (a.StartsWith("--waitForPid=", StringComparison.OrdinalIgnoreCase))
            {
                var value = a["--waitForPid=".Length..].Trim();
                return int.TryParse(value, out pid) && pid > 0;
            }

            if (string.Equals(a, "--waitForPid", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return int.TryParse(args[i + 1].Trim(), out pid) && pid > 0;
        }

        return false;
    }

    private void KillResidualAppProcesses()
    {
        var currentPid = Environment.ProcessId;

        foreach (var p in Process.GetProcessesByName(_serviceName))
        {
            if (p.Id == currentPid)
                continue;

            LogInfo($"Принудительное завершение остаточного процесса '{_serviceName}' PID={p.Id} (только процесс, без дерева — иначе убьёт текущий установщик).");
            p.Kill(entireProcessTree: false);
            p.WaitForExit(TimeSpan.FromSeconds(15));
            p.Dispose();
        }
    }

    private void BackupCurrentInstallation(string bin, string wwwroot, string backupRoot)
    {
        if (Directory.Exists(backupRoot))
            Directory.Delete(backupRoot, true);

        Directory.CreateDirectory(backupRoot);

        if (File.Exists(bin))
            File.Copy(bin, Path.Combine(backupRoot, _exeName), true);

        if (Directory.Exists(wwwroot))
            CopyFilesRecursively(wwwroot, Path.Combine(backupRoot, _wwwroot));
    }

    private async Task<bool> DeleteInstallationFilesAsync(string bin, string wwwroot)
    {
        var binOk = await DeleteFileWithRetryAsync(bin);
        var wwwOk = await DeleteDirectoryWithRetryAsync(wwwroot);
        return binOk && wwwOk;
    }

    private async Task<bool> DeleteFileWithRetryAsync(string filePath, int retries = 8, int delaySeconds = 3)
    {
        if (!File.Exists(filePath))
            return true;

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                File.Delete(filePath);
                LogInfo($"Файл удалён: {filePath}");
                return true;
            }
            catch (IOException ex)
            {
                LogInfo($"Попытка {attempt}/{retries} удалить файл {filePath} (IOException): {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogInfo($"Попытка {attempt}/{retries} удалить файл {filePath} (UnauthorizedAccessException): {ex.Message}");
            }

            KillResidualAppProcesses();

            if (attempt < retries)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        if (File.Exists(filePath))
        {
            LogError($"Не удалось удалить файл после {retries} попыток: {filePath}");
            return false;
        }

        return true;
    }

    private async Task<bool> DeleteDirectoryWithRetryAsync(string directoryPath, int retries = 8, int delaySeconds = 3)
    {
        if (!Directory.Exists(directoryPath))
            return true;

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                Directory.Delete(directoryPath, true);
                LogInfo($"Каталог удалён: {directoryPath}");
                return true;
            }
            catch (IOException ex)
            {
                LogInfo($"Попытка {attempt}/{retries} удалить каталог {directoryPath} (IOException): {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                LogInfo($"Попытка {attempt}/{retries} удалить каталог {directoryPath} (UnauthorizedAccessException): {ex.Message}");
            }

            KillResidualAppProcesses();

            if (attempt < retries)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        if (Directory.Exists(directoryPath))
        {
            LogError($"Не удалось удалить каталог после {retries} попыток: {directoryPath}");
            return false;
        }

        return true;
    }

    private async Task UpdateInstallParametersAsync(string[] installerArgs)
    {
        var xapikey = StringHelpers.ArgumentValue(installerArgs, "--xapikey", _configuration.OrganisationConfig.XapiKey());
        if (!string.IsNullOrWhiteSpace(xapikey))
            _configuration.OrganisationConfig.SetXapiKey(xapikey);

        await _parametersService.UpdateAsync(_configuration);
    }

    private void WriteChecksum(string[] installerArgs)
    {
        var checksum = StringHelpers.ArgumentValue(installerArgs, "--checksum", "");
        if (string.IsNullOrWhiteSpace(checksum))
            return;

        var dataFolder = Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName);
        Directory.CreateDirectory(dataFolder);
        var checkSumFileName = Path.Combine(dataFolder, "checksum.txt");
        File.WriteAllText(checkSumFileName, checksum);
    }

    private async Task<bool> RollbackInstallAsync(
        string bin,
        string wwwroot,
        string backupRoot,
        bool serviceExistedBeforeInstall,
        bool serviceWasRunningBeforeInstall)
    {
        LogInfo("Запуск rollback после ошибки установки.");

        using var currentService = GetExistingService();
        if (currentService is not null)
            StopService(currentService);

        KillResidualAppProcesses();
        await Task.Delay(TimeSpan.FromSeconds(2));

        await DeleteInstallationFilesAsync(bin, wwwroot);

        var backupBin = Path.Combine(backupRoot, _exeName);
        var backupWwwRoot = Path.Combine(backupRoot, _wwwroot);

        if (File.Exists(backupBin))
            File.Copy(backupBin, bin, true);

        if (Directory.Exists(backupWwwRoot))
            CopyFilesRecursively(backupWwwRoot, wwwroot);

        if (serviceExistedBeforeInstall && serviceWasRunningBeforeInstall)
        {
            using var restoredService = GetExistingService();
            if (restoredService is null)
            {
                LogError("Rollback: служба не найдена в SCM после восстановления файлов — запуск невозможен.");
                return false;
            }

            LogInfo($"Rollback: запуск службы '{_serviceName}' после восстановления файлов.");
            StartService(restoredService);
            LogInfo("Rollback: служба запущена.");
        }
        else if (serviceExistedBeforeInstall)
            LogInfo("Rollback: до обновления служба была остановлена — автозапуск после отката не выполняется.");

        DeleteBackup(backupRoot);
        return true;
    }

    private void DeleteBackup(string backupRoot)
    {
        if (Directory.Exists(backupRoot))
            Directory.Delete(backupRoot, true);
    }

    private void StartOperationLog(string operationName)
    {
        Directory.CreateDirectory(_logDirectory);
        _logFilePath = Path.Combine(_logDirectory, "updateLog.txt");
        File.WriteAllText(_logFilePath, string.Empty);
        WriteLog("INFO", $"Старт операции '{operationName}'.");
    }

    private void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    private void LogError(string message)
    {
        WriteLog("ERROR", message);
    }

    private void WriteLog(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{timestamp}][{level}] {message}{Environment.NewLine}";

        Console.Write(line);

        if (!string.IsNullOrWhiteSpace(_logFilePath))
            File.AppendAllText(_logFilePath, line);
    }
}
