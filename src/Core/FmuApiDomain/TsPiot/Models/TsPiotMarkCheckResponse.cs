using System.Text.Json.Serialization;

namespace FmuApiDomain.TsPiot.Models;

public record TsPiotMarkCheckResponse
{
    [JsonPropertyName("codesResponse")]
    public TsPiotCodesResponse Response { get; set; } = new();
}