namespace FmuApiDomain.Configuration.Options
{
    public class ServerConfig
    {
        public int ApiIpPort { get; set; } = 2578;
        public bool TsPiotEnabled { get; set; } = false;
        public int LocalModuleVersion { get; set; } = 0;
    }
}
