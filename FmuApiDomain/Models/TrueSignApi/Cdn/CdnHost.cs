using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.TrueSignApi.Cdn
{
    public class CdnHost
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;
    }
}
