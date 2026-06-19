using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record TsPiotInstancesInfoResponse
{
    [JsonPropertyName("instances")]
    public List<TsPiotInstanceListItem> Instances { get; set; } = [];
}

public record TsPiotInstanceListItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
