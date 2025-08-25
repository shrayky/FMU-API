using FmuApiApplication.Services.AcoUnit;
using FmuApiApplication.Workers;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Constants;
using LocalModuleIntegration;
using Serilog;
using Shared.FilesFolders;
using Shared.Logging;
using System.Net;
using TrueApiCdn;


namespace WebApi.Extensions
{
    public static class WebHostExtensions
    {
        public static WebApplicationBuilder ApplyAppConfigurationExtension(this WebApplicationBuilder builder)
        {
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var configService = scope.ServiceProvider.GetRequiredService<IParametersService>();
            var settings = configService.Current();

            builder.WebHost.UseUrls($"http://+:{settings.ServerConfig.ApiIpPort}");

            ConfigureCors(builder.Services, settings.ServerConfig.ApiIpPort);

            //if (settings.TrueSignTokenService.ConnectionAddres != string.Empty)
            //    builder.Services.AddHostedService<TrueSignTokenServiceLoaderWorker>();

            if (settings.HostsToPing.Count > 0)
            {
                builder.Services.AddHttpClient("internetCheck");
                builder.Services.AddHostedService<InternetConnectionCheckWorker>();
            }

            builder.Services.AddHttpClient<AlcoUnitGateway>("alcoUnit", options =>
            {
                options.BaseAddress = new Uri(settings.FrontolAlcoUnit.NetAdres);
                options.Timeout = TimeSpan.FromSeconds(20);
            });

            TrueApiRegistration.AddService(builder.Services);
            LocalModuleRegistration.AddService(builder.Services);

            ConfigureLogging(builder, settings.Logging);

            return builder;
        }

        private static void ConfigureLogging(WebApplicationBuilder builder, LogSettings settings)
        {
            if (!settings.IsEnabled)
                return;

            string logFolder = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                logFolder = Path.Combine(Folders.LogFolder(),
                                         ApplicationInformation.Manufacture, ApplicationInformation.AppName,
                                         "log");
            }
            else if (OperatingSystem.IsLinux())
            {
                logFolder = Path.Combine(Folders.LogFolder(),
                                         ApplicationInformation.Manufacture.ToLower(),
                                         ApplicationInformation.AppName.ToLower());
            }

            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);

            string logFileName = Path.Combine(logFolder, $"{ApplicationInformation.AppName.ToLower()}.log");

            builder.Logging.AddSerilog(SerilogConfiguration.LogToFile(settings.LogLevel, logFileName, settings.LogDepth));
        }

        private static void ConfigureCors(IServiceCollection services, int ipPort)
        {
            List<string> hostAdreses = [];

            hostAdreses.Add($"http://{Dns.GetHostName()}:{ipPort}");
            hostAdreses.Add($"http://localhost:{ipPort}");
            hostAdreses.Add($"http://127.0.0.1:{ipPort}");

            services.AddCors(opt =>
            {
                opt.AddDefaultPolicy(
                    policy =>
                    {
                        policy.WithOrigins(hostAdreses.ToArray()).AllowAnyMethod();
                    });
            });
        }

    }
}
