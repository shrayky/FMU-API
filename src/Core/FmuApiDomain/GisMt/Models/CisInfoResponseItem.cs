using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

public class CisInfoResponseItem
{
    [JsonPropertyName("cisInfo")]
    public CisInfoData? CisInfo { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }
}

public class CisInfoData
{
    [JsonPropertyName("requestedCis")]
    public string? RequestedCis { get; set; }

    [JsonPropertyName("cis")]
    public string? Cis { get; set; }

    [JsonPropertyName("gtin")]
    public string? Gtin { get; set; }

    [JsonPropertyName("printView")]
    public string? PrintView { get; set; }

    [JsonPropertyName("ownerInn")]
    public string? OwnerInn { get; set; }

    [JsonPropertyName("ownerName")]
    public string? OwnerName { get; set; }

    [JsonPropertyName("producerInn")]
    public string? ProducerInn { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("productGroup")]
    public string? ProductGroup { get; set; }

    [JsonPropertyName("productGroupId")]
    public int ProductGroupId { get; set; }

    [JsonPropertyName("expirationDate")]
    public string? ExpirationDate { get; set; }

    [JsonPropertyName("expireDate")]
    public DateTime? ExpireDate { get; set; }

    [JsonPropertyName("markWithdraw")]
    public bool MarkWithdraw { get; set; }

    [JsonPropertyName("packageType")]
    public string? PackageType { get; set; }
}
