using System.Text.Json;
using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

public class GisMtDocInfoResponse
{
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("docDate")]
    public DateTime? DocDate { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("senderInn")]
    public string? SenderInn { get; set; }

    [JsonPropertyName("receiverInn")]
    public string? ReceiverInn { get; set; }

    [JsonPropertyName("input")]
    public bool Input { get; set; }

    [JsonPropertyName("productGroup")]
    public JsonElement? ProductGroup { get; set; }

    [JsonPropertyName("body")]
    public JsonElement? Body { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("cisesList")]
    public List<string>? CisesList { get; set; }
}
