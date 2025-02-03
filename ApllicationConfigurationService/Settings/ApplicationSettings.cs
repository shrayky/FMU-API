using FmuApiDomain.Configuration.Options.Organisation;
using FmuApiDomain.Configuration.Options.TrueSign;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration;
using System.Text.Json.Serialization;
using FmuApiSettings;

namespace ApplicationConfigurationService.Settings
{
    public class ApplicationSettings
    {
        public string AppName { get; } = ApplicationInformationConstants.AppName;
        public int AppVersion { get; set; } = ApplicationInformationConstants.AppVersion;
        public int Assembly { get; set; } = ApplicationInformationConstants.Assembly;
        public string NodeName { get; set; } = string.Empty;
        public ServerConfig ServerConfig { get; set; } = new();
        public List<StringValue> HostsToPing { get; set; } = [];
        public MinimalPrices MinimalPrices { get; set; } = new();
        public OrganisationConfigurution OrganisationConfig { get; set; } = new();
        public AlcoUnitConfig FrontolAlcoUnit { get; set; } = new();
        public CouchDbConnection Database { get; set; } = new();
        public TokenService TrueSignTokenService { get; set; } = new();
        public HttpRequestTimeouts HttpRequestTimeouts { get; set; } = new();
        public LogSettings Logging { get; set; } = new();
        public FrontolConnectionSettings FrontolConnectionSettings { get; set; } = new();
        public SaleControlConfig SaleControlConfig { get; set; } = new();
        public CentralServerConnectionSettings CentralServerConnectionSettings { get; set; } = new();
        [JsonInclude]
        public AutoUpdateOptions AutoUpdate { get; private set; } = AutoUpdateOptions.Create();

        // устаревшие параметры
        public string? HostToPing { get; set; }
        public string? XAPIKEY { get; set; }
        //public List<TrueSignCdn>? Cdn { get; set; }
        public SignData? SignData { get; set; }
    }
}
