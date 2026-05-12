using System.Text.Json.Serialization;

namespace FmuApiDomain.TsPiot.Models;

public class TsPiotOnlineCheckResponseV3
{
    [JsonPropertyName("codesResponse")]
    public List<TsPiotCodesResponseItem> CodesResponseItems { get; set; } = [];
}
