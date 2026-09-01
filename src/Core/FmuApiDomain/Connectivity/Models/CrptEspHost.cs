using System.Text.Json.Serialization;

namespace FmuApiDomain.Connectivity.Models;

public record CrptEspHost
{
    public CrptEspHost(string url, string group)
    {
        Url = url;
        Group = group;
    }

    [JsonPropertyName("url")]
    public string Url = string.Empty;

    [JsonPropertyName("group")]
    public string Group = string.Empty;
}
