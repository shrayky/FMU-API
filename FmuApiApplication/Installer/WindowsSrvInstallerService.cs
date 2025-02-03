using FmuApiDomain.Configuration;
using FmuApiSettings;
using Shared.Strings;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;

namespace FmuApiApplication.Installer
{
    public class WindowsSrvInstallerService
    {
        private readonly IParametersService _parametersService;

        private readonly string _serviceName = ApplicationInformationConstants.AppName.ToLower();
        private readonly string _serviceDisplayName = ApplicationInformationConstants.ServiceName;
        private readonly string _installDirectory = string.Empty;
        private readonly Parameters _configuration;

        public WindowsSrvInstallerService(IParametersService parametersService)
        {
            _parametersService = parametersService;
            
            _installDirectory = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "", "Program Files", "Automation", "FMU-API");
            
            _configuration = _parametersService.Current();
        }

        public async Task<bool> InstallAsync(string[] installerArgs)
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

            string serviceFileName = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
            string setupFolder = Path.GetDirectoryName(serviceFileName) ?? serviceFileName.Replace("fmu-api.exe", "");

            if (Directory.Exists(_installDirectory))
                Directory.Delete(_installDirectory);

            CopyFilesRecursively(setupFolder, _installDirectory);

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

                //$"netsh advfirewall firewall show rule name = {_serviceName}"

                startInfo.Arguments = $"/c netsh advfirewall firewall delete rule name = \"{_serviceName}\"";
                process.Start();

                startInfo.Arguments = $"/c netsh advfirewall firewall add rule name = \"{_serviceName}\" dir =in action = allow protocol = TCP localport = 2578";
                process.Start();
            }

            var xapikey = StringHelpers.ArgumentValue(installerArgs, "--xapikey", _configuration.OrganisationConfig.XapiKey());

            _configuration.OrganisationConfig.SetXapiKey(xapikey);

            await _parametersService.UpdateAsync(_configuration);

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

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

    }
}
