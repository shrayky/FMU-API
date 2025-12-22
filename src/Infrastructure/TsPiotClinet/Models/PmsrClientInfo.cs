using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record PmsrClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}