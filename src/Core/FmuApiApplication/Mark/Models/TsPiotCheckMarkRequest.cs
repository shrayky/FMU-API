using System.Text.Json.Serialization;

namespace FmuApiApplication.Mark.Models;

public record TsPiotCheckMarkRequest
{
    [JsonPropertyName("codes")]
    public List<string> Codes { get; set; } = [];
    [JsonPropertyName("client_info")]
    public PmsrClientInfo ClientInfo { get; set; } = new();
}