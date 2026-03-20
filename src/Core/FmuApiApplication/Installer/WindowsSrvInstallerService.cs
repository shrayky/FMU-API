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

namespace FmuApiApplication.Installer;

[SupportedOSPlatform("windows")]
public class WindowsSrvInstallerService
{
    private readonly IParametersService _parametersService;

    private readonly string _serviceName = ApplicationInformation.AppName.ToLower();
    private readonly string _serviceDisplayName = ApplicationInformation.ServiceName;
    private readonly string _installDirectory;
    private readonly Parameters _configuration;

    public WindowsSrvInstallerService(IParametersService parametersService)
    {
        _parametersService = parametersService;
        
        _installDirectory = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "",
                                        "Program Files",
                                        ApplicationInformation.Manufacture,
                                        ApplicationInformation.AppName);
        
        _configuration = _parametersService.Current();
    }

    private static void CopyFilesRecursively(string sourcePath, string targetPath)
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

        if (!Directory.Exists(_installDirectory))
            Directory.CreateDirectory(_installDirectory);

        var bin = Path.Combine(_installDirectory, "fmu-api.exe");
        var wwwroot = Path.Combine(_installDirectory, "wwwroot");

        var existingService = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == _serviceName);

        if (existingService != null)
        {
            StopService(existingService);

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        var serviceFileName = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
        var setupFolder = Path.GetDirectoryName(serviceFileName) ?? serviceFileName.Replace("fmu-api.exe", "");

        var binDelete = await DeleteFileWithRetry(bin);
        var wwwRootDelete = await DeleteDirectoryWithRetry(wwwroot);

        if (!(binDelete && wwwRootDelete))
        {
            existingService?.Dispose();
            return false;
        }

        CopyFilesRecursively(setupFolder, _installDirectory);

        using var process = new Process();
        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        
        if (existingService is null)
        {
            process.StartInfo = startInfo;
            startInfo.FileName = "cmd.exe";

            startInfo.Arguments = $"/c sc create {_serviceName} binPath= \"{bin} --service\" DisplayName= \"{_serviceDisplayName}\" type= own start= auto";
            process.Start();

            startInfo.Arguments = $"/c sc failure \"{_serviceName}\" reset= 5 actions= restart/5000";
            process.Start();

            //$"netsh advfirewall firewall show rule name = {_serviceName}"

            startInfo.Arguments = $"/c netsh advfirewall firewall delete rule name = \"{_serviceName}\"";
            process.Start();

            startInfo.Arguments = $"/c netsh advfirewall firewall add rule name = \"{_serviceName}\" dir =in action = allow protocol = TCP localport = 2578";
            process.Start();

            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        var xapikey = StringHelpers.ArgumentValue(installerArgs, "--xapikey", _configuration.OrganisationConfig.XapiKey());

        if (!String.IsNullOrEmpty(xapikey))
            _configuration.OrganisationConfig.SetXapiKey(xapikey);

        await _parametersService.UpdateAsync(_configuration);

        if (existingService == null)
            existingService = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == _serviceName);

        if (existingService != null)
        {
            if (existingService.Status != ServiceControllerStatus.Running)
            {
                existingService.Start();

                try
                {
                    existingService.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
                }
                catch
                {}
            }
        }

        var checksum = StringHelpers.ArgumentValue(installerArgs, "--checksum", "");

        if (checksum != string.Empty)
        {
            var dataFolder = Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName);
            var checkSumFileName = Path.Combine(dataFolder, "checksum.txt");
            File.WriteAllText(checkSumFileName, checksum);
        }

        startInfo.Arguments = $"/c net start {_serviceName}";
        process.Start();

        existingService?.Dispose();

        return true;

    }

    private void StopService(ServiceController service)
    {
        if (service.Status == ServiceControllerStatus.Stopped)
            return;

        if (service.Status == ServiceControllerStatus.Running)
        {
            service.Stop();
        }

        try
        {
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(1));
        }
        catch
        {
            KillService();
        }
    }

    private void KillService()
    {
        var currentPid = Environment.ProcessId;

        foreach (var p in Process.GetProcessesByName(_serviceName))
        {
            try
            {
                if (p.Id == currentPid)
                    continue;

                p.Kill(true);
                p.WaitForExit(TimeSpan.FromSeconds(5));
            }
            finally
            {
                p.Dispose();
            }
        }
    }

    public bool Uninstall()
    {
        Unregister();

        var bin = Path.Combine(_installDirectory, "fmu-api.exe");
        var wwwroot = Path.Combine(_installDirectory, "wwwroot");

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

        var existingService = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == _serviceName);

        using var process = new Process();
        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (existingService != null)
        {
            StopService(existingService);
        }
        else
        {
            var bin = Path.Combine(_installDirectory, "fmu-api.exe");
            
            process.StartInfo = startInfo;
            startInfo.FileName = "cmd.exe";

            startInfo.Arguments = $"/c sc create {_serviceName} binPath= \"{bin} --service\" DisplayName= \"{_serviceDisplayName}\" type= own start= auto";
            process.Start();

            startInfo.Arguments = $"/c sc failure \"{_serviceName}\" reset= 5 actions= restart/5000";
            process.Start();

            //$"netsh advfirewall firewall show rule name = {_serviceName}"

            startInfo.Arguments = $"/c netsh advfirewall firewall delete rule name = \"{_serviceName}\"";
            process.Start();

            startInfo.Arguments = $"/c netsh advfirewall firewall add rule name = \"{_serviceName}\" dir =in action = allow protocol = TCP localport = 2578";
            process.Start();
        }

        startInfo.Arguments = $"/c sc start {_serviceName}";
        process.Start();

        existingService?.Dispose();

        return true;
    }

    public bool Unregister()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return false;

        var existingService = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == _serviceName);

        if (existingService is null)
            return true;

        StopService(existingService);

        using var process = new Process();
        ProcessStartInfo startInfo = new()
        {
            WindowStyle = ProcessWindowStyle.Hidden
        };

        process.StartInfo = startInfo;
        startInfo.FileName = "cmd.exe";

        startInfo.Arguments = $"/c sc delete {_serviceName}";
        process.Start();

        existingService?.Dispose();

        return true;
    }

    private static async Task<bool> DeleteFileWithRetry(string filePath, int retries = 5, int delaySeconds = 5)
    {
        if (!File.Exists(filePath))
            return true;

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                File.Delete(filePath);
                Console.WriteLine($"[Installer] Файл удален: {filePath}");
                return true;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[Installer] Попытка {attempt}/{retries} удалить файл {filePath} (IOException): {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[Installer] Попытка {attempt}/{retries} удалить файл {filePath} (UnauthorizedAccessException): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Installer] Неожиданная ошибка при удалении {filePath}: {ex}");
                return false;
            }

            if (attempt < retries)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        if (File.Exists(filePath))
        {
            Console.WriteLine($"[Installer] Не удалось удалить файл после {retries} попыток: {filePath}");
            return false;
        }

        return true;
    }

    private static async Task<bool> DeleteDirectoryWithRetry(string directoryPath, int retries = 5, int delaySeconds = 5)
    {
        if (!Directory.Exists(directoryPath))
            return true;

        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                Directory.Delete(directoryPath, true);
                Console.WriteLine($"[Installer] Каталог удален: {directoryPath}");
                return true;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[Installer] Попытка {attempt}/{retries} удалить каталог {directoryPath} (IOException): {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"[Installer] Попытка {attempt}/{retries} удалить каталог {directoryPath} (UnauthorizedAccessException): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Installer] Неожиданная ошибка при удалении {directoryPath}: {ex}");
                return false;
            }

            if (attempt < retries)
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        if (Directory.Exists(directoryPath))
        {
            Console.WriteLine($"[Installer] Не удалось удалить каталог после {retries} попыток: {directoryPath}");
            return false;
        }

        return true;
    }

}
