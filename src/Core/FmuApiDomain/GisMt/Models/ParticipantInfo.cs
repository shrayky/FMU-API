using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

public class ParticipantInfo
{
    [JsonPropertyName("inn")]
    public string Inn { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("productGroups")]
    public List<string> ProductGroups { get; set; } = [];
}
