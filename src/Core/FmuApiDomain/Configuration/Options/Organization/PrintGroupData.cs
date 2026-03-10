using FmuApiDomain.Fmu.Document;

namespace FmuApiDomain.Configuration.Options.Organization;

public class PrintGroupData
{
    public int Id { get; set; } = 0;
    public string XAPIKEY { get; set; } = string.Empty;
    public string INN { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public LocalModuleConnection LocalModuleConnection { get; set; } = new();
    public TsPiotConnectionSettings TsPiot { get; set; } = new();
    public TrueApiIntegrationSettings TrueApiIntegrationSettings { get; set; } = new();
}
