using Serilog;
using Serilog.Events;

namespace Shared.Logging
{
    public class SerilogConfiguration
    {
        private static RollingInterval rollingInterval = RollingInterval.Day;

        public static ILogger LogToFile(string logLevel, string logFileName, int logDepth)
        {
            logLevel ??= "information";

            ArgumentException.ThrowIfNullOrEmpty(logFileName, "logFileName");
            ArgumentException.ThrowIfNullOrWhiteSpace(logFileName, "logFileName");

            logDepth = logDepth == 0 ? 30 : logDepth;

            var loggerConfiguration = new LoggerConfiguration()
                .WriteTo.File(logFileName,
                              rollOnFileSizeLimit: true,
                              rollingInterval: rollingInterval,
                              retainedFileCountLimit: logDepth,
                              shared: true);

            loggerConfiguration = logLevel.ToLower() switch
            {
                "verbose" => loggerConfiguration.MinimumLevel.Verbose(),
                "debug" => loggerConfiguration.MinimumLevel.Debug(),
                "information" => loggerConfiguration.MinimumLevel.Information(),
                "warning" => loggerConfiguration.MinimumLevel.Warning(),
                "error" => loggerConfiguration.MinimumLevel.Error(),
                "fatal" => loggerConfiguration.MinimumLevel.Fatal(),
                _ => loggerConfiguration.MinimumLevel.Information()
            };

            // Фильтры для инфраструктурных логов
            loggerConfiguration = loggerConfiguration
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Error)
                .MinimumLevel.Override("System", LogEventLevel.Error);

            return loggerConfiguration.CreateLogger();
        }
    }
}
