using System.Text.Json.Serialization;

namespace FmuApiDomain.SoftwareUpdate.Models;

/// Файл архива обновления в релизе.
public class ReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;
}
