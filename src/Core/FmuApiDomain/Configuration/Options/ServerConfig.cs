namespace FmuApiDomain.Configuration.Options;

public class ServerConfig
{
    public int ApiIpPort { get; set; } = 2578;
    public bool TsPiotEnabled { get; set; } = true;
    public int LocalModuleVersion { get; set; } = 2;
}
