using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options;
using FmuApiSettings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;

namespace AutoUpdateWorkerService
{
    public class AutoUpdateWorker : BackgroundService
    {
        private readonly ILogger<AutoUpdateWorker> _logger;

        public AutoUpdateWorker(ILogger<AutoUpdateWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(60_000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                    break;

                Result updateResult = СheckUpdates();

                if (updateResult.IsSuccess)
                    break;
            }
        }

        private Result СheckUpdates()
        {
            AutoUpdateOptions options = Constants.Parametrs.AutoUpdate;

            if (!options.Enabled)
                return Result.Failure("Not enabled");

            if (!(options.FromHour <= DateTime.Now.Hour && options.CanUpdateUntill() > DateTime.Now.Hour))
                return Result.Failure("Not now");

            string updateFileName = Path.Combine(options.UpdateFilesCatalog, "update.zip");

            if (!File.Exists(updateFileName))
                return Result.Failure("No updates");

            if (OperatingSystem.IsWindows())
            {
                _logger.LogInformation("Найдено обновление, запускаю установку.");

                var installerPath = Path.Combine(Path.GetTempPath(), Constants.Parametrs.AppName);

                if (Directory.Exists(installerPath))
                    Directory.Delete(installerPath, true);

                Directory.CreateDirectory(installerPath);

                ZipFile.ExtractToDirectory(updateFileName, installerPath);

                File.Delete(updateFileName);

                Process process = new();
                ProcessStartInfo startInfo = new()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    CreateNoWindow = true,
                    Arguments = $"/c {installerPath}\\{Constants.Parametrs.AppName}.exe --install",
                    RedirectStandardOutput = true,
                };

                process.StartInfo = startInfo;
                process.Start();
            }

            return Result.Success();

        }

        public static void AddService(IServiceCollection services)
        {
            services.AddHostedService<AutoUpdateWorker>();
        }
    }
}
