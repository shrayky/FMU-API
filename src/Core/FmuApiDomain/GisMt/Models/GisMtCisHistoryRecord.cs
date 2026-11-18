using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Событие ответа True API cises/history: положение кода и документ, сменивший статус.
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

    [JsonPropertyName("ownerName")]
    public string? OwnerName { get; set; }

    [JsonPropertyName("operationDate")]
    public string? OperationDate { get; set; }

    [JsonPropertyName("docId")]
    public string? DocId { get; set; }
}
