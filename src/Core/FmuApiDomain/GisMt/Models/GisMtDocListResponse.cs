using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

public class GisMtDocListResponse
{
    [JsonPropertyName("results")]
    public List<GisMtDocListItem> Results { get; set; } = [];

    [JsonPropertyName("nextPage")]
    public bool NextPage { get; set; }
}

public class GisMtDocListItem
{
    [JsonPropertyName("number")]
    public string Number { get; set; } = string.Empty;

    [JsonPropertyName("docDate")]
    public DateTime? DocDate { get; set; }

    [JsonPropertyName("receivedAt")]
    public DateTime? ReceivedAt { get; set; }

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
    public string ProductGroup { get; set; } = string.Empty;

    [JsonPropertyName("productGroupId")]
    public int ProductGroupId { get; set; }
}
