using System.Text.Json.Serialization;

namespace FmuApiDomain.DTO.BeerTaps;

public record BeerOnTap
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("markCode")]
    public string MarkCode { get; set; } = string.Empty;

    [JsonPropertyName("kegVolume")]
    public int Volume { get; set; } = 0;

    [JsonPropertyName("wareName")]
    public string WareName { get; set; } = string.Empty;

    [JsonPropertyName("wareCode")]
    public string WareCode { get; set; } = string.Empty;

    [JsonPropertyName("tapName")]
    public string TapName { get; set; } = string.Empty;

    [JsonPropertyName("sales")]
    public int Sales { get; set; } = 0;
}
