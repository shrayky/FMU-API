using ApplicationConfigurationService;
using AutoUpdateWorkerService;
using CentralServerExchange;
using CouchDb;
using FmuApiApplication.Documents;
using FmuApiApplication.Mark;
using FmuApiApplication.Services.AcoUnit;
using FmuApiApplication.Services.Installer;
using FmuApiApplication.Services.MarkServices;
using FmuApiApplication.Services.TrueSign;
using FmuApiApplication.Workers;
using FmuApiSettings;
using FrontolDb;
using Microsoft.AspNetCore.Mvc.Controllers;
using Scalar.AspNetCore;
using Serilog;
using Shared.Strings;
using WebApi.Extensions;

var slConsole = new LoggerConfiguration()
    .MinimumLevel.Debug().WriteTo
                 .Console()
                 .CreateLogger();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(slConsole);
});

ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

if (OperatingSystem.IsLinux())
    RunHttpApiService();

if (OperatingSystem.IsWindows())
{
    _ = (args.Length == 0 ? "" : args[0]) switch
    {
        "--service" => RunHttpApiService(),
        "--install" => await InstallAsWindowsServiceAsync(),
        "--uninstall" => UninstallWindowsService(),
        _ => ShowAppInfo()
    };
}

bool RunHttpApiService()
{
    string dataFolder = StringHelpers.ArgumentValue(args, "--dataFolder", "");
    Constants.Init(dataFolder);

    WebApplicationBuilder? builder = WebApplication.CreateBuilder();
    var services = builder.Services;

    services.AddControllers();
    services.AddRazorPages();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    builder.Services.AddMemoryCache();

    ApplicationConfiguration.AddService(services);

    builder.ApplyAppConfigurationExtension();

    services.Configure<RouteOptions>(option =>
    {
        option.AppendTrailingSlash = true;
        option.LowercaseQueryStrings = true;
        option.LowercaseUrls = true;
    });

    services.AddHttpClient();

    services.AddScoped<MarksCheckService>();
    services.AddScoped<MarkStateSrv>();
    services.AddScoped<ProductInfo>();
    services.AddScoped<AlcoUnitGateway>();
    services.AddHostedService<CdnLoaderWorker>();
    
    CouchDbService.AddService(services);
    FrontolDbService.AddService(services);
    CentralServerExchangeWorker.AddService(services);
    AutoUpdateWorker.AddService(services);

    services.AddMarkServices();
    services.AddTransient<FrontolDocumentServiceFactory>();

    ConfigureOpenApi(services);

    if (OperatingSystem.IsWindows())
    {
        builder.Host.UseWindowsService();
    }

    var app = builder.Build();

    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
  
    app.UseCors();

    app.UseStaticFiles();
    app.UseRouting();

    app.MapRazorPages();
    app.MapControllers();

    app.Run();

    return true;
}

void ConfigureOpenApi(IServiceCollection services)
{
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
                return [api.GroupName];
            }

            ControllerActionDescriptor? controllerActionDescriptor = api.ActionDescriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                return [controllerActionDescriptor.ControllerName];
            }

            throw new InvalidOperationException("Unable to determine tag for endpoint.");
        });
        option.DocInclusionPredicate((name, api) => true);
    });
}



bool ShowAppInfo()
{
    logger.LogInformation("Ключи запуска приложения:\r\n" +
            "  --service запуск в режиме службы. рабочий режим\r\n" +
            "    --dataFolder катлог хранения настроек и лога (необязательный)\r\n" +
            "  --install запуск установки или обноления службы\r\n" +
            "    --xapikey параметр установки - записывает в настройки ключ для Честного знака (необязательный)\r\n" +
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