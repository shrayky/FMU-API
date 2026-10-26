using FmuApiDomain.Attributes;
using FmuApiDomain.TsPiot.Models;

namespace FmuApiDomain.Configuration.Options.Organization;

public class PrintGroupData
{
    [ExchangeableSetting]
    public int Id { get; set; } = 0;

    [ExchangeableSetting]
    public string XAPIKEY { get; set; } = string.Empty;

    [ExchangeableSetting]
    public bool XApiKeyExpired { get; set; }

    [ExchangeableSetting]
    public string INN { get; set; } = string.Empty;

    [ExchangeableSetting]
    public string Name { get; set; } = string.Empty;

    [ExchangeableSetting]
    public LocalModuleConnection LocalModuleConnection { get; set; } = new();

    [ExchangeableSetting]
    public TsPiotConnectionSettings TsPiot { get; set; } = new();

    public TrueApiIntegrationSettings TrueApiIntegrationSettings { get; set; } = new();
}
