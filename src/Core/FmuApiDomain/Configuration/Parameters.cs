using FmuApiDomain.Authentication.Models;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Constants;
using System.Text.Json.Serialization;
using FmuApiDomNewDirectory1ain.Configuration;

namespace FmuApiDomain.Configuration
{
    public class Parameters
    {
        public string AppName { get; } = ApplicationInformation.AppName;
        public int AppVersion { get; set; } = ApplicationInformation.AppVersion;
        public int Assembly { get; set; } = ApplicationInformation.Assembly;
        public string NodeName { get; set; } = string.Empty;
        public ServerConfig ServerConfig { get; set; } = new();
        public List<StringValue> HostsToPing { get; set; } = [];
        public MinimalPrices MinimalPrices { get; set; } = new();
        public OrganizationConfiguration OrganisationConfig { get; set; } = new();
        public AlcoUnitConfig FrontolAlcoUnit { get; set; } = new();
        public CouchDbConnection Database { get; set; } = new();
        public TokenServiceConfiguration TrueSignTokenService { get; set; } = new();
        public HttpRequestTimeouts HttpRequestTimeouts { get; set; } = new();
        public LogSettings Logging { get; set; } = new();
        public FrontolConnectionSettings FrontolConnectionSettings { get; set; } = new();
        public SaleControlConfig SaleControlConfig { get; set; } = new();
        public CentralServerConnectionProperties FmuApiCentralServer { get; set; } = new();
        [JsonInclude]
        public AutoUpdateOptions AutoUpdate { get; private set; } = AutoUpdateOptions.Create();

        // устаревшие параметры
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HostToPing { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? XAPIKEY { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TokenData? SignData { get; set; }

        [JsonConstructor]
        public Parameters()
        {

        }

    }
}
