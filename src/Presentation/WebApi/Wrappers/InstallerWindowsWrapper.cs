using ApplicationConfigurationService;
using FmuApiApplication.Installer;
using FmuApiApplication.Services.State;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using System.Runtime.Versioning;

namespace WebApi.Services;

[SupportedOSPlatform("windows")]
public static class InstallerWindowsWrapper
{
    public static async Task<bool> InstallAsWindowsServiceAsync(string[] args)
    { 
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMemoryCache();
                services.AddSingleton<IParametersService, SimpleParametersService>();
                services.AddSingleton<WindowsSrvInstallerService>();
                services.AddSingleton<IApplicationState, ApplicationState>();
            });

        using var host = builder.Build();
        var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();

        return await installerService.InstallAsync(args);
    }

    public static bool RegisterWindowsService(string[] args)
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMemoryCache();
                services.AddSingleton<IParametersService, SimpleParametersService>();
                services.AddSingleton<WindowsSrvInstallerService>();
                services.AddSingleton<IApplicationState, ApplicationState>();
            });

        using var host = builder.Build();
        var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();

        return installerService.RegisterWindowsService(args);
    }

    public static bool UninstallWindowsService()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMemoryCache();
                services.AddSingleton<IParametersService, SimpleParametersService>();
                services.AddSingleton<WindowsSrvInstallerService>();
                services.AddSingleton<IApplicationState, ApplicationState>();
            });

        using var host = builder.Build();
        var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();

        return installerService.Uninstall();
    }

    public static bool UnregisterWindowsService()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddMemoryCache();
                services.AddSingleton<IParametersService, SimpleParametersService>();
                services.AddSingleton<WindowsSrvInstallerService>();
                services.AddSingleton<IApplicationState, ApplicationState>();
            });

        using var host = builder.Build();
        var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();

        return installerService.Unregister();
    }
}
