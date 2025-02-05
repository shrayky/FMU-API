namespace FmuApiDomain.Configuration.Options
{
    public class LogSettings
    {
        public bool IsEnabled { get; set; } = true;
        public string LogLevel { get; set; } = "Warning";
        public int LogDepth { get; set; } = 30;
    }
}
