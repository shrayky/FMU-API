using System.Text.Json.Serialization;

namespace FmuApiDomain.CentralServiceExchange.Models.Answer;

public record FmuApiCentralResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("settingsUpdateAvailable")]
    public bool SettingsUpdateAvailable { get; init; }

    [JsonPropertyName("softwareUpdateAvailable")]
    public bool SoftwareUpdateAvailable { get; init; }

    [JsonPropertyName("updateHash")]
    public string UpdateHash { get; init; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("centralServerProperties")]
    public CentralServerProperties? CentralServerProperties { get; init; }
}
