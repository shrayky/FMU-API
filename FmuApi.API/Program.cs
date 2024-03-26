using FmuApiApplication.Services.Fmu;
using FmuApiApplication.Services.Installer;
using FmuApiApplication.Services.TrueSign;
using FmuApiApplication.Workers;
using Microsoft.AspNetCore.Mvc.Controllers;
using CouchDB.Driver.DependencyInjection;
using FmuApiCouhDb;
using FmuApiCouhDb.CrudServices;
using FmuApiApplication.Services.AcoUnit;
using FmuApiSettings;
using FmuApiApplication.Utilites;
using Serilog;
using FmuApiAPI;

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

    if (Constants.Parametrs.Logging.IsEnabled)
    {
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

    builder.WebHost.UseUrls(
        Constants.Parametrs.ServerConfig.Https switch
        {
            true => $"https://+:{Constants.Parametrs.ServerConfig.IpPort}",
            _ => $"http://+:{Constants.Parametrs.ServerConfig.IpPort}"
        });

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

    services.AddHttpClient<AlcoUnitGateway>("alcoUnit", options =>
    {
        options.BaseAddress = new Uri(Constants.Parametrs.FrontolAlcoUnit.NetAdres);
        options.Timeout = TimeSpan.FromSeconds(20);
    });
    services.AddSingleton<AlcoUnitGateway>();

    services.AddHostedService<CdnLoaderWorker>();

    if (Constants.Parametrs.TrueSignTokenService.ConnectionAddres != string.Empty)
        services.AddHostedService<TrueSignTokenServiceLoaderWorker>();

    if (Constants.Parametrs.HostToPing != string.Empty)
    {
        services.AddHttpClient("internetCheck", client =>
        {
            client.BaseAddress = new Uri(Constants.Parametrs.HostToPing);   
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHostedService<InternetConnectionCheckWorker>();
    }

    services.AddHostedService<ClearOldLogsWorker>();

    services.AddSingleton<MarkStateCrud>();

    if (Constants.Parametrs.MarksDb.ConfigurationEnabled())
    {
        services.AddCouchContext<CouchDbContext>(opt =>
            opt.UseEndpoint(Constants.Parametrs.MarksDb.NetAdres)
                .UseCookieAuthentication(Constants.Parametrs.MarksDb.UserName, Constants.Parametrs.MarksDb.Password)
                .EnsureDatabaseExists());
    }

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

    if (Constants.Parametrs.ServerConfig.Https)
        app.UseHttpsRedirection();

    app.Run();

    return true;
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