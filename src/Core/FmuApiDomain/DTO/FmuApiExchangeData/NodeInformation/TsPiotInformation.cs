namespace FmuApiDomain.DTO.FmuApiExchangeData.NodeInformation;

public record TsPiotInformation
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int ProtocolVersion { get; set; } = 0;
    public bool Online { get; set; } = false;
    public DateTime LastCheckTime { get; set; }
    public string Version { get; set; } = string.Empty;
}
