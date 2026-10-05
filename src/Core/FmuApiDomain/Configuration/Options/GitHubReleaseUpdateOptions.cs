namespace FmuApiDomain.Configuration.Options;

/// Настройки автообновления из релиза GitHub.
public class GitHubReleaseUpdateOptions
{
    public bool Enabled { get; set; }
    public int CheckIntervalMinutes { get; set; } = 120;
    public List<ScheduleTime> InstallSchedule { get; set; } = [];
}
