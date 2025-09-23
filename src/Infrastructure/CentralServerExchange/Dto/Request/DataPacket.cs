using System.Text.Json.Serialization;

namespace CentralServerExchange.Dto.Request;

public record DataPacket
{
    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;
    
    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;
}