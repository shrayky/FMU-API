using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;

namespace HostApp.Installer;

[SupportedOSPlatform("windows")]
internal sealed class WindowsHostInstaller
{
    private readonly string _installDirectory;
    private readonly string _logFilePath;

    public WindowsHostInstaller()
    {
        _installDirectory = Path.Combine(
            Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\",
            "Program Files",
            HostConstants.Manufacture,
            HostConstants.AppName);

        Directory.CreateDirectory(HostPaths.DataFolder);
        _logFilePath = Path.Combine(HostPaths.DataFolder, "updateLog.txt");
    }

    public async Task<int> InstallAsync(string[] args)
    {
        StartLog("install");
        LogInfo($"Аргументы: {string.Join(' ', args.Select(a => $"\"{a}\""))}");
        LogInfo($"Каталог установки: {_installDirectory}");

        try
        {
            await WaitForSourceProcessAsync(args);

            Directory.CreateDirectory(_installDirectory);

            StopAndKillService();

            MigrateFlatLayoutIfNeeded();

            var setupFolder = GetSetupFolder();
            LogInfo($"Каталог пакета: {setupFolder}");

            await InstallHostAsync(setupFolder);
            await InstallProductVersionsAsync(setupFolder);

            EnsureServiceRegistered();
            WriteChecksum(args);
            StartService();

            LogInfo("Установка завершена успешно.");
            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Ошибка установки: {ex}");
            return 1;
        }
    }

    public int Uninstall()
    {
        StartLog("uninstall");

        try
        {
            Unregister();

            var hostPath = Path.Combine(_installDirectory, HostConstants.HostExeName);
            DeleteFileWithRetry(hostPath);

            var fmuRoot = Path.Combine(_installDirectory, HostConstants.FmuProductName);
            DeleteDirectoryWithRetry(fmuRoot);

            // Остатки старой flat-схемы
            DeleteDirectoryWithRetry(Path.Combine(_installDirectory, "wwwroot"));

            LogInfo("Удаление завершено.");
            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Ошибка удаления: {ex}");
            return 1;
        }
    }

    public int Register()
    {
        StartLog("register");

        try
        {
            EnsureServiceRegistered();
            StartService();
            LogInfo("Регистрация завершена.");
            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Ошибка регистрации: {ex}");
            return 1;
        }
    }

    public int Unregister()
    {
        StartLog("unregister");

        try
        {
            StopAndKillService();

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c sc delete {HostConstants.ServiceName}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(30_000);

            LogInfo("Служба удалена из SCM.");
            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Ошибка unregister: {ex}");
            return 1;
        }
    }

    private async Task InstallHostAsync(string setupFolder)
    {
        var sourceHost = Path.Combine(setupFolder, HostConstants.HostExeName);
        if (!File.Exists(sourceHost))
            sourceHost = Environment.ProcessPath ?? sourceHost;

        if (!File.Exists(sourceHost))
            throw new FileNotFoundException("Не найден host exe в пакете.", HostConstants.HostExeName);

        var targetHost = Path.Combine(_installDirectory, HostConstants.HostExeName);
        if (string.Equals(Path.GetFullPath(sourceHost), Path.GetFullPath(targetHost), StringComparison.OrdinalIgnoreCase))
        {
            LogInfo("Host уже находится в каталоге установки — копирование пропущено.");
            return;
        }

        LogInfo($"Копирую host: {sourceHost} => {targetHost}");
        await CopyFileWithRetryAsync(sourceHost, targetHost);
    }

    private async Task InstallProductVersionsAsync(string setupFolder)
    {
        var packaged = FindPackagedVersions(setupFolder);
        if (packaged.Count == 0)
            throw new InvalidOperationException(
                $"В пакете нет продукта '{HostConstants.FmuProductName}'. Ожидается {HostConstants.FmuProductName}\\{{ver}}\\{HostConstants.FmuProductName}.exe или app\\{HostConstants.FmuProductName}.exe.");

        foreach (var (version, sourceDir) in packaged)
        {
            var versionFolder = ToVersionFolderName(version);
            var targetDir = Path.Combine(_installDirectory, HostConstants.FmuProductName, versionFolder);
            var partialDir = targetDir + ".partial";

            LogInfo($"Устанавливаю {HostConstants.FmuProductName} {versionFolder} из {sourceDir}");

            if (Directory.Exists(partialDir))
                DeleteDirectoryWithRetry(partialDir);

            CopyProductPayload(sourceDir, partialDir, setupFolder);

            if (Directory.Exists(targetDir))
                DeleteDirectoryWithRetry(targetDir);

            Directory.Move(partialDir, targetDir);
            LogInfo($"Версия {version} установлена в {targetDir}");
        }

        await Task.CompletedTask;
    }

    private List<(Version Version, string SourceDir)> FindPackagedVersions(string setupFolder)
    {
        var result = new List<(Version, string)>();
        var productRoot = Path.Combine(setupFolder, HostConstants.FmuProductName);

        if (Directory.Exists(productRoot))
        {
            foreach (var versionDir in Directory.EnumerateDirectories(productRoot))
            {
                var name = Path.GetFileName(versionDir);
                if (!Version.TryParse(name, out var version))
                    continue;

                var exe = Path.Combine(versionDir, $"{HostConstants.FmuProductName}.exe");
                if (!File.Exists(exe))
                {
                    LogInfo($"Пропуск {versionDir}: нет {HostConstants.FmuProductName}.exe");
                    continue;
                }

                result.Add((version, versionDir));
            }
        }

        if (result.Count > 0)
            return result;

        // Альтернатива: app\fmu.exe (+ wwwroot)
        var appDir = Path.Combine(setupFolder, "app");
        var appExe = Path.Combine(appDir, $"{HostConstants.FmuProductName}.exe");
        if (File.Exists(appExe))
        {
            var version = ReadFileVersion(appExe) ?? new Version(0, 0);
            result.Add((version, appDir));
            return result;
        }

        // Альтернатива: fmu.exe в корне пакета
        var rootExe = Path.Combine(setupFolder, $"{HostConstants.FmuProductName}.exe");
        if (File.Exists(rootExe))
        {
            var version = ReadFileVersion(rootExe) ?? new Version(0, 0);
            result.Add((version, setupFolder));
        }

        return result;
    }

    private void MigrateFlatLayoutIfNeeded()
    {
        var rootHost = Path.Combine(_installDirectory, HostConstants.HostExeName);
        var rootWww = Path.Combine(_installDirectory, "wwwroot");
        var productRoot = Path.Combine(_installDirectory, HostConstants.FmuProductName);

        var hasNewLayout = Directory.Exists(productRoot) &&
                           Directory.EnumerateDirectories(productRoot).Any(d =>
                               Version.TryParse(Path.GetFileName(d), out _) &&
                               File.Exists(Path.Combine(d, $"{HostConstants.FmuProductName}.exe")));

        if (hasNewLayout)
        {
            LogInfo("Новая раскладка уже есть — миграция flat не нужна.");
            // Удаляем возможный wwwroot в корне
            if (Directory.Exists(rootWww))
            {
                LogInfo("Удаляю устаревший корневой wwwroot.");
                DeleteDirectoryWithRetry(rootWww);
            }
            return;
        }

        if (!File.Exists(rootHost) && !Directory.Exists(rootWww))
        {
            LogInfo("Нет flat-установки для миграции.");
            return;
        }

        var version = File.Exists(rootHost)
            ? ReadFileVersion(rootHost) ?? new Version(0, 0)
            : new Version(0, 0);

        var versionFolder = ToVersionFolderName(version);
        var targetDir = Path.Combine(productRoot, versionFolder);
        LogInfo($"Миграция flat => {targetDir}");

        Directory.CreateDirectory(targetDir);

        if (Directory.Exists(rootWww))
        {
            var destWww = Path.Combine(targetDir, "wwwroot");
            if (Directory.Exists(destWww))
                DeleteDirectoryWithRetry(destWww);
            Directory.Move(rootWww, destWww);
        }

        if (File.Exists(rootHost))
        {
            var destExe = Path.Combine(targetDir, $"{HostConstants.FmuProductName}.exe");
            // Старый payload назывался fmu-api.exe — переименовываем в fmu.exe
            File.Move(rootHost, destExe, overwrite: true);
        }

        LogInfo("Миграция flat завершена.");
    }

    private void EnsureServiceRegistered()
    {
        var bin = Path.Combine(_installDirectory, HostConstants.HostExeName);
        if (!File.Exists(bin))
            throw new FileNotFoundException("Host exe не найден в каталоге установки.", bin);

        using var existing = GetService();
        if (existing is not null)
        {
            LogInfo("Служба уже зарегистрирована — обновляю binPath.");
            RunCmd($"sc config {HostConstants.ServiceName} binPath= \"{bin} --service\"");
            return;
        }

        LogInfo("Регистрирую Windows-службу.");
        RunCmd($"sc create {HostConstants.ServiceName} binPath= \"{bin} --service\" DisplayName= \"{HostConstants.ServiceDisplayName}\" type= own start= auto");
        RunCmd($"sc failure \"{HostConstants.ServiceName}\" reset= 5 actions= restart/5000");
        RunCmd($"netsh advfirewall firewall delete rule name = \"{HostConstants.ServiceName}\"");
        RunCmd($"netsh advfirewall firewall add rule name = \"{HostConstants.ServiceName}\" dir =in action = allow protocol = TCP localport = {HostConstants.HttpPort}");
    }

    private void StartService()
    {
        using var service = GetService()
            ?? throw new InvalidOperationException($"Служба '{HostConstants.ServiceName}' не найдена.");

        if (service.Status == ServiceControllerStatus.Running)
        {
            LogInfo("Служба уже запущена.");
            return;
        }

        LogInfo("Запуск службы...");
        service.Start();
        try
        {
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
            LogInfo("Служба запущена.");
        }
        catch (Exception ex)
        {
            LogError($"Служба не перешла в Running за 1 минуту: {ex.Message}");
        }
    }

    private void StopAndKillService()
    {
        using var service = GetService();
        if (service is not null)
        {
            if (service.Status is not ServiceControllerStatus.Stopped and not ServiceControllerStatus.StopPending)
            {
                LogInfo("Остановка службы...");
                try
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    LogInfo($"Штатная остановка не удалась: {ex.Message}. Принудительное завершение.");
                }
            }
        }

        KillResidualProcesses(HostConstants.ServiceName);
        KillResidualProcesses(HostConstants.FmuProductName);
    }

    private void KillResidualProcesses(string processName)
    {
        var currentPid = Environment.ProcessId;

        foreach (var p in Process.GetProcessesByName(processName))
        {
            try
            {
                if (p.Id == currentPid)
                    continue;

                LogInfo($"Завершаю остаточный процесс {processName} PID={p.Id}");
                // Без дерева — иначе можно убить сам установщик, если он дочерний
                p.Kill(entireProcessTree: false);
                p.WaitForExit(15_000);
            }
            catch (Exception ex)
            {
                LogInfo($"Не удалось завершить PID={p.Id}: {ex.Message}");
            }
            finally
            {
                p.Dispose();
            }
        }
    }

    private async Task WaitForSourceProcessAsync(string[] args)
    {
        var raw = CliArgs.Value(args, "--waitForPid", "");
        if (!int.TryParse(raw, out var pid) || pid <= 0 || pid == Environment.ProcessId)
        {
            LogInfo("Ожидание --waitForPid пропущено.");
            return;
        }

        LogInfo($"Ожидаю завершения PID={pid}...");
        try
        {
            using var process = Process.GetProcessById(pid);
            var exited = await Task.Run(() => process.WaitForExit(120_000));
            if (!exited)
                LogError($"PID={pid} не завершился за 120с — продолжаю установку.");
            else
                LogInfo($"PID={pid} завершён.");
        }
        catch (ArgumentException)
        {
            LogInfo($"PID={pid} уже не существует.");
        }
    }

    private void WriteChecksum(string[] args)
    {
        var checksum = CliArgs.Value(args, "--checksum", "");
        if (string.IsNullOrWhiteSpace(checksum))
            return;

        var path = Path.Combine(HostPaths.DataFolder, "checksum.txt");
        File.WriteAllText(path, checksum);
        LogInfo($"Записан checksum: {path}");
    }

    private static ServiceController? GetService() =>
        ServiceController.GetServices().FirstOrDefault(s =>
            string.Equals(s.ServiceName, HostConstants.ServiceName, StringComparison.OrdinalIgnoreCase));

    private static string GetSetupFolder()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
            return Path.GetDirectoryName(processPath) ?? AppContext.BaseDirectory;

        return AppContext.BaseDirectory;
    }

    private static Version? ReadFileVersion(string exePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);
            if (Version.TryParse(info.FileVersion, out var version))
                return new Version(version.Major, version.Minor);

            if (info.FileMajorPart > 0 || info.FileMinorPart > 0)
                return new Version(info.FileMajorPart, info.FileMinorPart);
        }
        catch
        {
            // ignore
        }

        return null;
    }

    /// <summary>
    /// Имя каталога версии: только major.minor (11.12, 12.1).
    /// </summary>
    private static string ToVersionFolderName(Version version) =>
        $"{version.Major}.{version.Minor}";

    private void CopyProductPayload(string sourceDir, string targetDir, string setupFolder)
    {
        var normalizedSource = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedSetup = Path.GetFullPath(setupFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var isPackageRoot = string.Equals(normalizedSource, normalizedSetup, StringComparison.OrdinalIgnoreCase);

        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, HostConstants.HostExeName, StringComparison.OrdinalIgnoreCase))
                continue;

            File.Copy(file, Path.Combine(targetDir, name), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var name = Path.GetFileName(dir);
            if (isPackageRoot &&
                string.Equals(name, HostConstants.FmuProductName, StringComparison.OrdinalIgnoreCase))
                continue;

            CopyDirectoryRecursive(dir, Path.Combine(targetDir, name));
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), overwrite: true);

        foreach (var dir in Directory.GetDirectories(sourceDir))
            CopyDirectoryRecursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
    }

    private static async Task CopyFileWithRetryAsync(string source, string target, int retries = 5)
    {
        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                File.Copy(source, target, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < retries)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            catch (UnauthorizedAccessException) when (attempt < retries)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }

        File.Copy(source, target, overwrite: true);
    }

    private static bool DeleteFileWithRetry(string path, int retries = 5)
    {
        if (!File.Exists(path))
            return true;

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                File.Delete(path);
                return true;
            }
            catch when (attempt < retries)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        return !File.Exists(path);
    }

    private static bool DeleteDirectoryWithRetry(string path, int retries = 5)
    {
        if (!Directory.Exists(path))
            return true;

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return true;
            }
            catch when (attempt < retries)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }

        return !Directory.Exists(path);
    }

    private static void RunCmd(string command)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (process is null)
            throw new InvalidOperationException($"Не удалось запустить: {command}");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(60_000);

        // sc/netsh часто пишут в stdout даже при успехе
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"Команда завершилась с кодом {process.ExitCode}: {command}. {stdout} {stderr}");
    }

    private void StartLog(string operation)
    {
        File.WriteAllText(_logFilePath, string.Empty);
        LogInfo($"Старт операции '{operation}'.");
    }

    private void LogInfo(string message) => WriteLog("INFO", message);

    private void LogError(string message) => WriteLog("ERROR", message);

    private void WriteLog(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{level}] {message}{Environment.NewLine}";
        Console.Write(line);
        File.AppendAllText(_logFilePath, line);
    }
}
