using ApplicationConfigurationService;
using FmuApiApplication.Installer;
using FmuApiApplication.Services.State;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using System.Runtime.Versioning;

namespace WebApi.Services;

[SupportedOSPlatform("linux")]
public static class InstallerLinuxService
{
    public static void InstallAsSystemdDaemon()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMemoryCache();
                services.AddSingleton<IParametersService, SimpleParametersService>();
                services.AddSingleton<LinuxDaemonInstaller>();
                services.AddSingleton<IApplicationState, ApplicationState>();
            });

        using var host = builder.Build();
        var installerService = host.Services.GetRequiredService<LinuxDaemonInstaller>();

        installerService.Register();
    }
}
