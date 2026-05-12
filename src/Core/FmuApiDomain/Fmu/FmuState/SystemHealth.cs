using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.FmuState;

public record SystemHealth
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = "regular";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
