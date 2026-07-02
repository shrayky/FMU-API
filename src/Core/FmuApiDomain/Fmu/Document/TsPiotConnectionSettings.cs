using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.Document;

public record TsPiotConnectionSettings
{
    [JsonPropertyName("host")]
    public string Host { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public string Port { get; set; } = string.Empty;

    [JsonPropertyName("informationPort")]
    public int InformationPort { get; set; } = 51077;

    [JsonPropertyName("informationEndpoint")]
    public string InformationEndpoint { get; set; } = "/api/v1/info";
}