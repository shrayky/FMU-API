using System.Text.Json.Serialization;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Событие движения кода для экрана проверки марки.
/// </summary>
public class MarkCheckMovement
{
    [JsonPropertyName("cis")]
    public string? Cis { get; set; }

    [JsonPropertyName("operationDate")]
    public string? OperationDate { get; set; }

    [JsonPropertyName("docId")]
    public string? DocId { get; set; }

    [JsonPropertyName("ownerInn")]
    public string? OwnerInn { get; set; }

    [JsonPropertyName("ownerName")]
    public string? OwnerName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("receipt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarkCheckReceipt? Receipt { get; set; }
}

/// <summary>
/// Свой чек продажи: адрес, время, ИНН продавца и касса.
/// </summary>
public class MarkCheckReceipt
{
    [JsonPropertyName("retailAddress")]
    public string? RetailAddress { get; set; }

    [JsonPropertyName("soldAt")]
    public string? SoldAt { get; set; }

    [JsonPropertyName("userInn")]
    public string? UserInn { get; set; }

    [JsonPropertyName("kktRegId")]
    public string? KktRegId { get; set; }
}
