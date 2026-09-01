using System.Text.Json.Serialization;

namespace FmuApiDomain.Connectivity.Models;

public class CrptEspCheckResult
{
    [JsonPropertyName("available")]
    public int Available { get; init; }

    [JsonPropertyName("unavailable")]
    public int Unavailable { get; init; }

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<CrptEspCheckItem> Items { get; init; } = [];
}
