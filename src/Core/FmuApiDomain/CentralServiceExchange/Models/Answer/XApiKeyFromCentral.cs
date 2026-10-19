using System.Text.Json.Serialization;

namespace FmuApiDomain.CentralServiceExchange.Models.Answer;

/// <summary>
/// x-api-key организации из ответа сеанса обмена с fmu-api-central.
/// </summary>
public record XApiKeyFromCentral
{
    [JsonPropertyName("inn")]
    public string Inn { get; init; } = string.Empty;

    [JsonPropertyName("xApiKey")]
    public string XApiKey { get; init; } = string.Empty;

    [JsonPropertyName("xApiKeyExpired")]
    public bool XApiKeyExpired { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; init; }
}
