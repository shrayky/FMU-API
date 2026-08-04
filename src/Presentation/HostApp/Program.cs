using HostApp;
using HostApp.Installer;
using HostApp.Services;
using HostApp.Workers;
using Serilog;

if (OperatingSystem.IsWindows() && args.Length > 0)
{
    var command = args[0];
    if (command is "--install" or "--uninstall" or "--register" or "--unregister")
    {
        var installer = new WindowsHostInstaller();
        var exitCode = command switch
        {
            "--install" => await installer.InstallAsync(args),
            "--uninstall" => installer.Uninstall(),
            "--register" => installer.Register(),
            "--unregister" => installer.Unregister(),
            _ => 1
        };

        return exitCode;
    }
}

Directory.CreateDirectory(HostPaths.LogFolder);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(HostPaths.LogFolder, "host-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();

try
{
    Log.Information("Старт HostApp ({Service})", HostConstants.ServiceDisplayName);

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = HostConstants.ServiceName;
    });

    builder.Services.AddSerilog();
    builder.Services.AddSingleton<ProductDiscovery>();
    builder.Services.AddSingleton<ChildProcessSupervisor>();
    builder.Services.AddSingleton<VersionCleanup>();
    builder.Services.AddHostedService<ProductsHostWorker>();

    var host = builder.Build();
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "HostApp аварийно завершился");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
