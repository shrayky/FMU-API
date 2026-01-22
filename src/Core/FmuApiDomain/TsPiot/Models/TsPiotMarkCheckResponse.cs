using System.Text.Json.Serialization;

namespace FmuApiDomain.TsPiot.Models;

public record TsPiotMarkCheckResponse
{
    [JsonPropertyName("codesResponse")]
    public TsPiotCodesResponse Response { get; set; } = new();
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public int Code { get; set; } = 0;
}