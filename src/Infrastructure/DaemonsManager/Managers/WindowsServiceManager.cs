using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using FmuApiDomain.DaemonsManager;

namespace ServicesAndDaemonsManager.Managers
{
    public class WindowsServiceManager : IDaemonManager
    {
        public bool IsRunning(string serviceName)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;
            
            try
            {
                using var service = new ServiceController(serviceName);
                return service.Status == ServiceControllerStatus.Running;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public string Status(string serviceName)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Не верная целевая операционная система";
            try
            {
                using var service = new ServiceController(serviceName);
                
                return service.Status.ToString();
            }
            catch (Exception)
            {
                return "Не найдена";
            }
        }

        public bool Restart(string serviceName)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/create /tn \"Restart{serviceName}\" /tr \"sc restart {serviceName}\" /sc once /st {DateTime.Now.AddSeconds(10):HH:mm:ss} /f",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            try
            {
                using var process = Process.Start(startInfo);

                if (process is null)
                    return false;
                
                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
