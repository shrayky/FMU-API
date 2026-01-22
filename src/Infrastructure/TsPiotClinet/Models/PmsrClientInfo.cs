using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record PmsrClientInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "FrontolMU";
    [JsonPropertyName("version")]
    public string Version { get; set; } = "4.4.5.767";
    [JsonPropertyName("id")]
    public string Id { get; set; } = "65329da4-01d1-48ba-9db8-ed58b3892774";
    [JsonPropertyName("token")]
    public string Token { get; set; } = "9b15f05a-11d9-4fd7-9025-daa5d867a35";
}