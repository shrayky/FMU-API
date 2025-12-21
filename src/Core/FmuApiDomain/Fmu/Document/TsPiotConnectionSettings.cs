using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.Document;

public record TsPiotConnectionSettings
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;
    
    [JsonPropertyName("port")]
    public string Port { get; set; } = string.Empty;
}