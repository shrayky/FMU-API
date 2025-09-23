using System.Text.Json.Serialization;

namespace CentralServerExchange.Dto.Request;

public record LocalModuleInformation
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
    
    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;
    
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;
    
    [JsonPropertyName("lastSync")]
    public long LastSync { get; init; } =  long.MinValue;
    
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
    
    [JsonPropertyName("operationMode")]
    public string OperationMode { get; init; } = string.Empty;
}