using System.Text.Json.Serialization;

namespace FmuApiDomain.Connectivity.Models;

public class CrptEspCheckItem
{
    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; init; } = string.Empty;

    [JsonPropertyName("available")]
    public bool? Available { get; init; }

    [JsonPropertyName("elapsedMs")]
    public int? ElapsedMs { get; init; }
}
