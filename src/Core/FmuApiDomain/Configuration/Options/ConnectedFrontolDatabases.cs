namespace FmuApiDomain.Configuration.Options;

public record ConnectedFrontolDatabases
{
    public SyncBeerTaps SyncBeerTapsSettings { get; set; } = new();

    public int PrintGroupSourseId { get; set; }

    public List<FrontolConnectionSettings> ConnectionSettings { get; set; } = [];
}

public record SyncBeerTaps
{
    public bool SyncBeerTapsEnabled { get; set; }
    public int SyncBeerTapsPeriodSeconds { get; set; } = 30;
}
