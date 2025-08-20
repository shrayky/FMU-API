using ApplicationConfigurationService;
using AutoUpdateWorkerService;
using CentralServerExchange;
using CouchDb;
using FmuApiApplication.Documents;
using FmuApiApplication.Installer;
using FmuApiApplication.Mark;
using FmuApiApplication.Services.AcoUnit;
using FmuApiApplication.Services.MarkServices;
using FmuApiApplication.Services.State;
using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using FrontolDb;
using MemoryCache;
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
        "--register" => RegisterWindowsService(),
        "--uninstall" => UninstallWindowsService(),
        "--unregister" => UnregisterWindowsService(),
        _ => ShowAppInfo()
    };
}

bool RunHttpApiService()
{
    string dataFolder = StringHelpers.ArgumentValue(args, "--dataFolder", "");
    
    WebApplicationBuilder? builder = WebApplication.CreateBuilder();
    var services = builder.Services;

    services.AddControllers();
    services.AddRazorPages();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    builder.Services.AddMemoryCache();

    ApplicationConfiguration.AddService(services);
    builder.Services.AddSingleton<IApplicationState, ApplicationState>();

    builder.ApplyAppConfigurationExtension();

    services.Configure<RouteOptions>(option =>
    {
        option.AppendTrailingSlash = true;
        option.LowercaseQueryStrings = true;
        option.LowercaseUrls = true;
    });

    services.AddHttpClient();

    services.AddScoped<MarksCheckService>();
    services.AddScoped<ProductInfo>();
    services.AddScoped<AlcoUnitGateway>();
    
    CouchDbServicesRegistration.AddService(services);
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

    //builder.Services.AddHostedService<StartWorker>();

    var app = builder.Build();

    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
  
    app.UseCors();

    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = prm =>
        {
            prm.Context.Response.Headers.Append("Cache-Control", "publc, max-age=864000");
        }
    });
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
            "    --dataFolder каталог хранения настроек и лога (необязательный)\r\n" +
            "  --install запуск установки или обновления службы с копированием файлов\r\n" +
            "    --xapikey параметр установки - записывает в настройки ключ для Честного знака (необязательный)\r\n" +
            "  --uninstall удаление службы и файлов\r\n" +
            "  --register регистрация как службы windows\r\n" +
            "  --unregister удаление службы windows");
    return true;
}

async Task<bool> InstallAsWindowsServiceAsync()
{
    var builder = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddMemoryCache();
            services.AddSingleton<IParametersService, SimpleParametersService>();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<WindowsSrvInstallerService>();
            services.AddSingleton<IApplicationState, ApplicationState>();
        });

    using var host = builder.Build();
    var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();
  
    return await installerService.InstallAsync(args);
}

bool RegisterWindowsService()
{
    var builder = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddMemoryCache();
            services.AddSingleton<IParametersService, SimpleParametersService>();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<WindowsSrvInstallerService>();
            services.AddSingleton<IApplicationState, ApplicationState>();
        });

    using var host = builder.Build();
    var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();

    return installerService.RegisterWindowsService(args);
}

bool UninstallWindowsService()
{
    var builder = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddMemoryCache();
            services.AddSingleton<IParametersService, SimpleParametersService>();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<WindowsSrvInstallerService>();
            services.AddSingleton<IApplicationState, ApplicationState>();
        });

    using var host = builder.Build();
    var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();

    return installerService.Uninstall();
}

bool UnregisterWindowsService()
{
    var builder = Host.CreateDefaultBuilder()
        .ConfigureServices(services =>
        {
            services.AddMemoryCache();
            services.AddSingleton<IParametersService, SimpleParametersService>();
            services.AddSingleton<ICacheService, MemoryCacheService>();
            services.AddSingleton<WindowsSrvInstallerService>();
            services.AddSingleton<IApplicationState, ApplicationState>();
        });

    using var host = builder.Build();
    var installerService = host.Services.GetRequiredService<WindowsSrvInstallerService>();

    return installerService.Unregister();
}