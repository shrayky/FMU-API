using FmuApiDomain.Attributes;
using FmuApiDomain.Authentication.Models;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Constants;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration;

public class Parameters
{
    public string AppName { get; } = ApplicationInformation.AppName;

    public int AppVersion { get; set; } = ApplicationInformation.AppVersion;

    public int Assembly { get; set; } = ApplicationInformation.Assembly;

    public string NodeName { get; set; } = string.Empty;

    [ExchangeableSetting]
    public ServerConfig ServerConfig { get; set; } = new();

    [ExchangeableSetting]
    public List<StringValue> HostsToPing { get; set; } = [];

    [ExchangeableSetting]
    public MinimalPrices MinimalPrices { get; set; } = new();

    [ExchangeableSetting]
    public OrganizationConfiguration OrganisationConfig { get; set; } = new();

    [ExchangeableSetting]
    public AlcoUnitConfig FrontolAlcoUnit { get; set; } = new();

    public CouchDbConnection Database { get; set; } = new();

    [ExchangeableSetting]
    public StatisticsSettings Statistics { get; set; } = new();

    [ExchangeableSetting]
    public HttpRequestTimeouts HttpRequestTimeouts { get; set; } = new();

    [ExchangeableSetting]
    public LogSettings Logging { get; set; } = new();

    [ExchangeableSetting]
    public SaleControlConfig SaleControlConfig { get; set; } = new();

    [ExchangeableSetting]
    public GisMtSettings GisMtSettings { get; set; } = new();

    [ExchangeableSetting]
    public List<GisMtProductMapping> GisMtProductMappings { get; set; } = [];

    public CentralServerConnectionProperties FmuApiCentralServer { get; set; } = new();

    [JsonPropertyName("githubReleaseUpdate")]
    public GitHubReleaseUpdateOptions GitHubReleaseUpdate { get; set; } = new();

    [JsonInclude]
    public AutoUpdateOptions AutoUpdate { get; private set; } = AutoUpdateOptions.Create();

    [JsonInclude]
    public ConnectedFrontolDatabases ConnectedFrontolSettings { get; private set; } = new();


    [Obsolete]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FrontolConnectionSettings? FrontolConnectionSettings { get; set; } = new();

    [Obsolete]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HostToPing { get; set; }

    [Obsolete]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? XAPIKEY { get; set; }

    [Obsolete]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenData? SignData { get; set; }

    [Obsolete]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TokenServiceConfiguration TrueSignTokenService { get; set; } = new();

    [JsonConstructor]
    public Parameters()
    {

    }

}
