using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Запись ответа True API cises/history: текущее положение кода идентификации, включая упаковку.
/// </summary>
public class GisMtCisHistoryRecord
{
    [JsonPropertyName("cis")]
    public string? Cis { get; set; }

    [JsonPropertyName("gtin")]
    public string? Gtin { get; set; }

    [JsonPropertyName("packageType")]
    public string? PackageType { get; set; }

    [JsonPropertyName("parent")]
    public string? Parent { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("ownerInn")]
    public string? OwnerInn { get; set; }
}
