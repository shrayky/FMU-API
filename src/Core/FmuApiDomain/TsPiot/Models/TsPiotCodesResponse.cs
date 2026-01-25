using System.Text.Json.Serialization;

namespace FmuApiDomain.TsPiot.Models;

public record TsPiotCodesResponse
{
    [JsonPropertyName("codesResponse")]
    public List<TsPiotCodesResponseItem> CodesResponseItems { get; set; } = [];
}