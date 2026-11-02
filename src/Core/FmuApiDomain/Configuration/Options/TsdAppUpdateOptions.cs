namespace FmuApiDomain.Configuration.Options;

/// Настройки получения приложения FmuApiTSD из релиза.
public class TsdAppUpdateOptions
{
    public const string DefaultRepositoryUrl = "https://github.com/shrayky/fmu-api-tsd-app";

    public bool Enabled { get; set; }

    public int CheckIntervalMinutes { get; set; } = 120;

    public string RepositoryUrl { get; set; } = DefaultRepositoryUrl;

    public string ReleaseAddress() =>
        string.IsNullOrWhiteSpace(RepositoryUrl) ? DefaultRepositoryUrl : RepositoryUrl.Trim();
}
