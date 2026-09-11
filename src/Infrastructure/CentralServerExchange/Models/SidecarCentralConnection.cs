namespace CentralServerExchange.Models;

/// <summary>
/// Sidecar config.json: формат fc-agent (Address) и дистрибутива FMU-6 (serverAddress).
/// </summary>
public sealed class SidecarCentralConnection
{
    public string Address { get; set; } = string.Empty;

    public string ServerAddress { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public string ResolvedAddress()
    {
        if (!string.IsNullOrWhiteSpace(Address))
            return Address;

        return ServerAddress;
    }
}
