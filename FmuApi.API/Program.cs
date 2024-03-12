using FmuApiApplication;
using FmuApiApplication.Services.Fmu;
using FmuApiApplication.Services.Installer;
using FmuApiApplication.Services.TrueSign;
using FmuApiApplication.Workers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Console;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(i => i.ColorBehavior = LoggerColorBehavior.Enabled);
});

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

_ = (args.Length == 0 ? "" : args[0]) switch
{
    "--service" => RunHttpApiService(),
    "--install" => await InstallAsWindowsServiceAsync(),
    "--uninstall" => UninstallWindowsService(),
    _ => ShowAppInfo()
};

bool RunHttpApiService()
{
    Constants.Init();

    WebApplicationOptions webApplicationOptions = new()
    {
        ContentRootPath = AppContext.BaseDirectory,
        ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
    };

    WebApplicationBuilder? builder = WebApplication.CreateBuilder(webApplicationOptions);

    builder.WebHost.UseUrls($"http://+:{Constants.Parametrs.ServerConfig.IpPort}");

    var services = builder.Services;

    services.Configure<RouteOptions>(option =>
    {
        option.AppendTrailingSlash = true;
        option.LowercaseQueryStrings = true;
        option.LowercaseUrls = true;
    });

    services.AddHttpClient();

    services.AddSingleton<CheckMarks>();
    services.AddSingleton<FrontolDocument>();
    services.AddSingleton<ProductInfo>();

    services.AddHostedService<CdnLoaderWorker>();
    services.AddHostedService<InternetConnectionCheckWorker>();
    services.AddHostedService<TrueSignTokenServiceLoader>();
  
    services.AddSwaggerGen(option =>
    {
        option.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
        option.IgnoreObsoleteActions();
        option.IgnoreObsoleteProperties();
        option.CustomSchemaIds(type => type.FullName);
        option.TagActionsBy(api =>
        {
            if (api.GroupName != null)
            {
                return new[] { api.GroupName };
            }

            var controllerActionDescriptor = api.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                return new[] { controllerActionDescriptor.ControllerName };
            }

            throw new InvalidOperationException("Unable to determine tag for endpoint.");
        });
        option.DocInclusionPredicate((name, api) => true);
    });

    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    if (OperatingSystem.IsWindows())
    {
        builder.Host.UseWindowsService();
    }

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapControllers();

    app.Run();

    return true;
}

    bool ShowAppInfo()
{
    logger.LogInformation("Ключи запуска приложения:\r\n" +
            "  --service запуска в режиме службы. рабочий режим\r\n" +
            "  --install запуск установки или обнвления службы\r\n" +
            "    --xapikey параметр установки - записывает в настройки ключ для Честного знака\r\n" +
            "  --unisntall удаление службы");

    return true;
}

async Task<bool> InstallAsWindowsServiceAsync()
{
    WindowsInstallerService installerService = new(logger);
    return await installerService.InstallAsync(args);
}

bool UninstallWindowsService()
{
    WindowsInstallerService installerService = new(logger);
    return installerService.Uninstall();
}