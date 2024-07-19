using FmuApiApplication.Services.Fmu;
using FmuApiApplication.Services.Installer;
using FmuApiApplication.Services.TrueSign;
using FmuApiApplication.Workers;
using Microsoft.AspNetCore.Mvc.Controllers;
using CouchDB.Driver.DependencyInjection;
using FmuApiApplication.Services.AcoUnit;
using FmuApiSettings;
using FmuApiApplication.Utilites;
using Serilog;
using FmuApiAPI;
using FmuApiCouhDb;
using FmuApiCouhDb.CrudServices;
using System.Net;
using FmuFrontolDb;
using Microsoft.EntityFrameworkCore;
using FmuApiApplication.Services.Frontol;

var slConsole = new LoggerConfiguration()
    .MinimumLevel.Debug().WriteTo
                 .Console()
                 .CreateLogger();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(slConsole);
});

ILogger < Program> logger = loggerFactory.CreateLogger<Program>();

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

    services.Configure<RouteOptions>(option =>
    {
        option.AppendTrailingSlash = true;
        option.LowercaseQueryStrings = true;
        option.LowercaseUrls = true;
    });

    services.AddHttpClient();

    services.AddScoped<CheckMarks>();

    services.AddScoped<FrontolDocument>();
    services.AddScoped<ProductInfo>();

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

    if (Constants.Parametrs.FrontolConnectionSettings.ConnectionEnable())
    {
        services.AddDbContext<FrontolDbContext>(options =>
        {
            options.UseFirebird(Constants.Parametrs.FrontolConnectionSettings.ConnectionStringBuild());
        });
        
        services.AddScoped<FrontolSprtDataService>();

    } 

    services.AddHostedService<ClearOldLogsWorker>();

    services.AddScoped<MarkInformationCrud>();
    services.AddScoped<FrontolDocumentCrud>();

    ConfigureCors(services);

    if (Constants.Parametrs.Database.ConfigurationIsEnabled)
    {
        services.AddCouchContext<CouchDbContext>(opt =>
            opt.UseEndpoint(Constants.Parametrs.Database.NetAdres)
                .UseCookieAuthentication(Constants.Parametrs.Database.UserName, Constants.Parametrs.Database.Password)
                .EnsureDatabaseExists());
    }

    ConfigureSwagger(services);

    if (OperatingSystem.IsWindows())
    {
        builder.Host.UseWindowsService();
    }

    builder.Services.AddRazorPages();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

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

    string logFileName = string.Concat(Constants.DataFolderPath, "\\log\\fmu-api.log");

    var logConfig = Constants.Parametrs.Logging.LogLevel.ToLower() switch
    {
        "verbose" => LoggerConfig.Verbose(logFileName),
        "debug" => LoggerConfig.Debug(logFileName),
        "information" => LoggerConfig.Information(logFileName),
        "warning" => LoggerConfig.Warning(logFileName),
        "error" => LoggerConfig.Error(logFileName),
        "fatal" => LoggerConfig.Fatal(logFileName),
        _ => LoggerConfig.Information(logFileName)
    };

    builder.Logging.AddSerilog(logConfig);
}

void ConfigureSwagger(IServiceCollection services)
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

    services.AddControllers();
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
            "  --service запуска в режиме службы. рабочий режим\r\n" +
            "    --dataFolder катлог хранения настроек и лога (необязательный)" +
            "  --install запуск установки или обнвления службы\r\n" +
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