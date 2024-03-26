using Serilog;

namespace FmuApiAPI
{
    public static class LoggerConfig
    {
        public static Serilog.ILogger Verbose(string logFileName) =>
            new LoggerConfiguration().MinimumLevel.Verbose().WriteTo
                         .File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day)
                         .CreateLogger();

        public static Serilog.ILogger Debug(string logFileName) =>
            new LoggerConfiguration().MinimumLevel.Debug().WriteTo
                         .File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day)
                         .CreateLogger();

        public static Serilog.ILogger Information(string logFileName) =>
            new LoggerConfiguration().MinimumLevel.Information().WriteTo
                         .File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day)
                         .CreateLogger();

        public static Serilog.ILogger Warning(string logFileName) =>
            new LoggerConfiguration().MinimumLevel.Warning().WriteTo
                         .File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day)
                         .CreateLogger();

        public static Serilog.ILogger Error(string logFileName) =>
            new LoggerConfiguration().MinimumLevel.Error().WriteTo
                         .File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day)
                         .CreateLogger();

        public static Serilog.ILogger Fatal(string logFileName) =>
            new LoggerConfiguration().MinimumLevel.Fatal().WriteTo
                         .File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day)
                         .CreateLogger();

    }
}
