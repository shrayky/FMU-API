using System.Text.Json.Serialization;
using FmuApiDomain.Configuration.Options;

namespace CentralServerExchange.Dto.Request;

public record FmuApiSetting
{
    [JsonPropertyName("version")]
    public int Version { get; init; } = 0;
    
    [JsonPropertyName("assembly")]
    public int Assembly { get; init; } = 0;

    [JsonPropertyName("server")]
    public ServerConfiguration ServerConfiguration { get; init; } = new();

    [JsonPropertyName("hostsToPing")]
    public List<StringValue> HostsToPing { get; init; } = [];
    
    [JsonPropertyName("minimalPrices")]
    public MinimalPrices MinimalPrices { get; init; } = new();
    
    [JsonPropertyName("SalesControl")]
    public SaleControl  SaleControl { get; init; } = new();
    
    [JsonPropertyName("Organizations")]
    public List<Organization> Organizations { get; init; } = [];
    
    [JsonPropertyName("Database")]
    public Database Database { get; init; } = new();
    
    [JsonPropertyName("TokenService")]
    public TokenService TokenService { get; init; } = new();
    
    [JsonPropertyName("timeOut")]
    public TimeOutConfiguration TimeOut { get; init; } = new();
    
    [JsonPropertyName("logging")]
    public Logging Logging { get; init; } = new();
}

public record ServerConfiguration
{
    [JsonPropertyName("apiIpPort")]
    public int ApiIpPort { get; init; } = 0;
}

public record MinimalPrices
{
    [JsonPropertyName("tabaco")]
    public int Tabaco { get; init; }
}

public record Organization
{
    [JsonPropertyName("id")]
    public int Id { get; init; } = 0;
    
    [JsonPropertyName("INN")]
    public string Inn { get; init; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;
    
    [JsonPropertyName("xApiKey")]
    public string XApiKey { get; init; } = string.Empty;
}

public record Database
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = false;
}

public record TokenService
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = false;
    
    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;
}

public record TimeOutConfiguration
{
    [JsonPropertyName("cdnRequest")]
    public int CdnRequest { get; init; } = 15;
    
    [JsonPropertyName("trueSignCheckRequest")]
    public int  TrueSignCheckRequest { get; init; } = 2;
    
    [JsonPropertyName("internetConnectionCheck")]
    public int InternetConnectionCheck { get; init; } = 15;
}

public record Logging
{
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; init; }

    [JsonPropertyName("logLevel")]
    public string LogLevel { get; init; } = string.Empty;

    [JsonPropertyName("logDepth")]
    public int LogDepth { get; init; }
}

public record SaleControl
{
    [JsonPropertyName("banSalesReturnedWares")]
    public bool BanSalesReturnedWares { get; init; }

    [JsonPropertyName("ignoreVerificationErrorForTrueApiGroups")]
    public string IgnoreVerificationErrorForTrueApiGroups { get; init; } = string.Empty;

    [JsonPropertyName("checkReceiptReturn")]
    public bool CheckReceiptReturn { get; init; }

    [JsonPropertyName("correctExpireDateInSaleReturn")]
    public bool CorrectExpireDateInSaleReturn { get; init; }

    [JsonPropertyName("sendEmptyTrueApiAnswerWhenTimeoutError")]
    public bool SendEmptyTrueApiAnswerWhenTimeoutError { get; init; }

    [JsonPropertyName("checkIsOwnerField")]
    public bool CheckIsOwnerField { get; init; }

    [JsonPropertyName("sendLocalModuleInformationalInRequestId")]
    public bool SendLocalModuleInformationalInRequestId { get; init; }

    [JsonPropertyName("rejectSalesWithoutCheckInformationFrom")]
    public DateTime RejectSalesWithoutCheckInformationFrom { get; init; }

    [JsonPropertyName("resetSoldStatusForReturn")]
    public bool ResetSoldStatusForReturn { get; init; }
}