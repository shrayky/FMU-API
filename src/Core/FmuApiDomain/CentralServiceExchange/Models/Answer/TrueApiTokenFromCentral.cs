using System.Text.Json.Serialization;

namespace FmuApiDomain.CentralServiceExchange.Models.Answer;

/// <summary>
/// Токен True API из ответа сеанса обмена с fmu-api-central.
/// </summary>
public record TrueApiTokenFromCentral
{
    [JsonPropertyName("inn")]
    public string Inn { get; init; } = string.Empty;

    [JsonPropertyName("token")]
    public string Token { get; init; } = string.Empty;

    [JsonPropertyName("expired")]
    public DateTime Expired { get; init; }
}
