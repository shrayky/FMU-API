using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record TsPiotModuleSettings
{
    [JsonPropertyName("compatibilityMode")]
    public bool CompatibilityMode { get; set; } = true;

    [JsonPropertyName("allowRemoteConnection")]
    public bool AllowRemoteConnection { get; set; } = true;

    [JsonPropertyName("gismtAddress")]
    public string GisMtAddress { get; set; } = string.Empty;

    [JsonPropertyName("cdnCodesCheckTimeout")]
    public int CdnCodesCheckTimeout { get; set; } = 1500;

    [JsonPropertyName("cdnHealthCheckTimeout")]
    public int CdnHealthCheckTimeout { get; set; } = 2000;

    [JsonPropertyName("httpProxyType")]
    public string HttpProxyType { get; set; } = string.Empty;

    [JsonPropertyName("httpProxyHost")]
    public string HttpProxyHost { get; set; } = string.Empty;

    [JsonPropertyName("httpProxyPort")]
    public int HttpProxyPort { get; set; } = 0;
}