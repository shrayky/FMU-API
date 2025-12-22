using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record TsPiotCheckMarkRequest
{
    [JsonPropertyName("codes")]
    public List<string> Codes { get; set; } = [];
    [JsonPropertyName("client_info")]
    public PmsrClientInfo ClientInfo { get; set; } = new();
}