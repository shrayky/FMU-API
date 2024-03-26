using FmuApiApplication.Utilites;
using FmuApiSettings;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace FmuApiApplication.Services.Installer
{
    public class WindowsInstallerService
    {
        private readonly string _serviceName = "fmu-api";
        private readonly string _serviceDisplayName = "DS:FMU-API";
        private readonly string _installDirectory = string.Empty;

        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public WindowsInstallerService(Microsoft.Extensions.Logging.ILogger logger)
        {
            _installDirectory = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "", "Program Files", "Automation", "FMU-API");
            _logger = logger;
        }

        public async  Task<bool> InstallAsync(string[] installerArgs)
        {
            if (!Directory.Exists(_installDirectory))
                Directory.CreateDirectory(_installDirectory);

            var bin = Path.Combine(_installDirectory, "fmu-api.exe");

            ServiceController? existingService = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == _serviceName);

            if (existingService != null)
            {
                if (existingService.Status == ServiceControllerStatus.Running)
                {
                    existingService.Stop();
                    existingService.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }

            if (File.Exists(bin))
                File.Delete(bin);

            File.Copy(Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location, bin);

            if (existingService is null)
            {
                Process process = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                process.StartInfo = startInfo;
                startInfo.FileName = "cmd.exe";

                startInfo.Arguments = $"/c sc create {_serviceName} binPath= \"{bin} --service\" DisplayName= \"{_serviceDisplayName}\" type= own start= auto";
                process.Start();

                startInfo.Arguments = $"/c sc failure \"{_serviceName}\" reset= 5 actions= restart/5000";
                process.Start();

                startInfo.Arguments = $"/c netsh advfirewall firewall delete rule name = \"{_serviceName}\"";
                process.Start();

                startInfo.Arguments = $"/c netsh advfirewall firewall add rule name = \"{_serviceName}\" dir =in action = allow protocol = TCP localport = 2578";
                process.Start();
            }

            Constants.Init();
            Constants.Parametrs.XAPIKEY = StringHelper.ArgumentValue(installerArgs, "--xapikey", Constants.Parametrs.XAPIKEY);
            await Constants.Parametrs.SaveAsync(Constants.Parametrs, Constants.DataFolderPath);

            return true;

        }

        public bool Uninstall()
        {
            ServiceController? existingService = ServiceController.GetServices().FirstOrDefault(ser => ser.ServiceName == _serviceName);

            if (existingService is null)
                return true;

            if (existingService.Status == ServiceControllerStatus.Running)
            {
                existingService.Stop();
                existingService.WaitForStatus(ServiceControllerStatus.Stopped);
            }

            Process process = new();
            ProcessStartInfo startInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };

            process.StartInfo = startInfo;
            startInfo.FileName = "cmd.exe";

            startInfo.Arguments = $"/c sc delete {_serviceName}";
            process.Start();

            var bin = Path.Combine(_installDirectory, "fmu-api.exe");

            if (File.Exists(bin))
                File.Delete(bin);

            return true;

        }
        
    }
}
