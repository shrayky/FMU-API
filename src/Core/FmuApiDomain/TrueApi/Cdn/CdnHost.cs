using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.Cdn
{
    public class CdnHost
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;
    }
}
