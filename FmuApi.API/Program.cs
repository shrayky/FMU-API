using ApllicationConfigurationService;
using AutoUpdateWorkerService;
using CentralServerExchange;
using CouchDb;
using FmuApiApplication.Services.AcoUnit;
using FmuApiApplication.Services.Fmu;
using FmuApiApplication.Services.Fmu.Documents;
using FmuApiApplication.Services.Installer;
using FmuApiApplication.Services.MarkServices;
using FmuApiApplication.Services.TrueSign;
using FmuApiApplication.Utilites;
using FmuApiApplication.Workers;
using FmuApiDomain.Cache;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiSettings;
using FrontolDb;
using LoggerConfig;
using MemoryCache;
using Microsoft.AspNetCore.Mvc.Controllers;
using Scalar.AspNetCore;
using Serilog;
using System.Net;

var slConsole = new LoggerConfiguration()
    .MinimumLevel.Debug().WriteTo
                 .Console()
                 .CreateLogger();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(slConsole);
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
    string dataFolder = StringHelper.ArgumentValue(args, "--dataFolder", "");
    Constants.Init(dataFolder);

    WebApplicationOptions webApplicationOptions = new()
    {
        ContentRootPath = AppContext.BaseDirectory,
        ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
    };

    WebApplicationBuilder? builder = WebApplication.CreateBuilder(webApplicationOptions);

    ConfigureLogging(builder);

    builder.WebHost.UseUrls($"http://+:{Constants.Parametrs.ServerConfig.ApiIpPort}");

    var services = builder.Services;

    services.AddControllers();
    builder.Services.AddMemoryCache();

    builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

    services.Configure<RouteOptions>(option =>
    {
        option.AppendTrailingSlash = true;
        option.LowercaseQueryStrings = true;
        option.LowercaseUrls = true;
    });

    services.AddHttpClient();

    services.AddScoped<MarksChekerService>();

    // устарело
    //services.AddScoped<FrontolDocument>();
    
    services.AddScoped<ProductInfo>();

    services.AddScoped<MarkStateSrv>();

    services.AddHttpClient<AlcoUnitGateway>("alcoUnit", options =>
    {
        options.BaseAddress = new Uri(Constants.Parametrs.FrontolAlcoUnit.NetAdres);
        options.Timeout = TimeSpan.FromSeconds(20);
    });
    services.AddScoped<AlcoUnitGateway>();

    services.AddHostedService<CdnLoaderWorker>();
    
    if (Constants.Parametrs.TrueSignTokenService.ConnectionAddres != string.Empty)
        services.AddHostedService<TrueSignTokenServiceLoaderWorker>();

    if (Constants.Parametrs.HostsToPing.Count > 0)
    {
        services.AddHttpClient("internetCheck");
        services.AddHostedService<InternetConnectionCheckWorker>();
    }

    ConfigureCors(services);

    ApplicationConfiguration.AddService(services);

    CouchDbService.AddService(services);
    FrontolDbService.AddService(services);
    CentralServerExchangeWorker.AddService(services);
    AutoUpdateWorker.AddService(services);

    services.AddScoped<IMarkInformationService, MarkInformationService>();
    services.AddTransient<FrontolDocumentServiceFactory>();

    

    ConfigureOpenApi(services);

    if (OperatingSystem.IsWindows())
    {
        builder.Host.UseWindowsService();
    }

    builder.Services.AddRazorPages();

    var app = builder.Build();

    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
    
    //app.UseSwaggerUI();

    app.UseCors();

    app.UseStaticFiles();
    app.UseRouting();

    app.MapRazorPages();
    app.MapControllers();

    app.Run();

    return true;
}

void ConfigureLogging(WebApplicationBuilder builder)
{
    if (!Constants.Parametrs.Logging.IsEnabled)
        return;

    string logFileName = string.Concat(Constants.DataFolderPath, "\\log\\", Constants.Parametrs.AppName.ToLower(), ".log");;

    builder.Logging.AddSerilog(SerilogConfiguration.LogToFile(Constants.Parametrs.Logging.LogLevel, logFileName, Constants.Parametrs.Logging.LogDepth));
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

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
}

void ConfigureCors(IServiceCollection services)
{
    List<string> hostAdreses = [];

    hostAdreses.Add($"http://{Dns.GetHostName()}:{Constants.Parametrs.ServerConfig.ApiIpPort}");
    hostAdreses.Add($"http://localhost:{Constants.Parametrs.ServerConfig.ApiIpPort}");
    hostAdreses.Add($"http://127.0.0.1:{Constants.Parametrs.ServerConfig.ApiIpPort}");

    services.AddCors(opt =>
    {
        opt.AddDefaultPolicy(
            policy =>
            {
                policy.WithOrigins(hostAdreses.ToArray()).AllowAnyMethod();
            });
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