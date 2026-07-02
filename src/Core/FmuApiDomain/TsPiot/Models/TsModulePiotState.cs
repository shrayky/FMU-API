namespace FmuApiDomain.TsPiot.Models;

public record TsModulePiotState
{
    public int Organization { get; set; } = 0;
    public string Connection { get; set; } = string.Empty;
    public int ProtocolVersion { get; set; } = 1;
    public bool Online { get; set; } = false;
    public DateTime LastCheck { get; set; } = DateTime.MinValue;
    public string Version { get; set; } = string.Empty;
    public DateTime? LicenseActiveTill { get; set; }
}
