using System.Text.Json.Serialization;

namespace FmuApiApplication.StateCollectors.Models;

public record LocalModuleStateInformation
{
    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; }  = string.Empty;

    [JsonPropertyName("lastSyncTime")]
    public DateTime LastSyncTime { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; }   = string.Empty;

    [JsonPropertyName("isReady")]
    public bool IsReady { get; init; }
    
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("lastSync")]
    public long LastSync { get; init; } = long.MinValue;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("operationMode")]
    public string OperationMode { get; init; } = string.Empty;
}