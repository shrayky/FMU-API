namespace FmuApiDomain.Configuration.Options;

public record LocalModuleGeneral
{
    public int Version { get; set; } = 2;
    public bool AutoInitializeOnSyncError { get; set; } = true;
}
