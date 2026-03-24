using System.Text.Json.Serialization;

namespace FmuApiDomain.TsPiot.Models;

public record TsPiotMarkCheckResponse
{
    [JsonPropertyName("codesResponse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TsPiotCodesResponse? Response { get; set; }
    
    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Message { get; set; }
    
    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Code { get; set; }
}