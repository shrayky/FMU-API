using System.Text.Json.Serialization;

namespace TsPiotClinet.Models;

public record TsPiotInstanceDetailResponse
{
    [JsonPropertyName("licenses")]
    public List<TsPiotLicenseInfo> Licenses { get; set; } = [];

    [JsonPropertyName("regData")]
    public TsPiotRegData RegData { get; set; } = new();
}

public record TsPiotLicenseInfo
{
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("activeTill")]
    public string ActiveTill { get; set; } = string.Empty;
}

public record TsPiotRegData
{
    [JsonPropertyName("kktInn")]
    public string KktInn { get; set; } = string.Empty;
}
