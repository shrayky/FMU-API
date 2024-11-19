using Serilog;

namespace FmuApiAPI
{
    public static class LoggerConfig
    {
        public static Serilog.ILogger Verbose(string logFileName, int? logFilesCountLimit) =>
            new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day, retainedFileCountLimit: logFilesCountLimit, shared: true)
                .CreateLogger();

        public static Serilog.ILogger Debug(string logFileName, int? logFilesCountLimit) =>
            new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day, retainedFileCountLimit: logFilesCountLimit, shared: true)
                .CreateLogger();

        public static Serilog.ILogger Information(string logFileName, int? logFilesCountLimit) =>
            new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day, retainedFileCountLimit: logFilesCountLimit, shared: true)
                .CreateLogger();

        public static Serilog.ILogger Warning(string logFileName, int? logFilesCountLimit) =>
            new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day, retainedFileCountLimit: logFilesCountLimit, shared: true)
                .CreateLogger();

        public static Serilog.ILogger Error(string logFileName, int? logFilesCountLimit) =>
            new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day, retainedFileCountLimit: logFilesCountLimit, shared: true)
                .CreateLogger();

        public static Serilog.ILogger Fatal(string logFileName, int? logFilesCountLimit) =>
            new LoggerConfiguration()
                .MinimumLevel.Fatal()
                .WriteTo.File(logFileName, rollOnFileSizeLimit: true, rollingInterval: RollingInterval.Day, retainedFileCountLimit: logFilesCountLimit, shared: true)
                .CreateLogger();

    }
}
