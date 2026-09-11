using System.Text.Json.Serialization;

namespace FmuApiApplication.TrueApi.Models;

/// <summary>
/// Статус токена True API организации для мониторинга и списка организаций.
/// </summary>
public record TrueApiTokenStateInformation
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("organization")]
    public int Organization { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("inn")]
    public string Inn { get; init; } = string.Empty;

    [JsonPropertyName("loaded")]
    public bool Loaded { get; init; }

    [JsonPropertyName("expired")]
    public DateTime? Expired { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}
