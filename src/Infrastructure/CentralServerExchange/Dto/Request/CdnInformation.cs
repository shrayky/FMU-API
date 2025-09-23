using System.Text.Json.Serialization;

namespace CentralServerExchange.Dto.Request;

public record CdnInformation
{
    [JsonPropertyName("host")]
    public string Host { get; init; } = string.Empty;

    [JsonPropertyName("latency")]
    public long Latency { get; init; }

    [JsonPropertyName("offline")]
    public bool Offline { get; init; }

    [JsonPropertyName("offlineFrom")]
    public DateTime OfflineFrom { get; init; }
}