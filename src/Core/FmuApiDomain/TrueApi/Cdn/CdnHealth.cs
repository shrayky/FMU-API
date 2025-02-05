using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.Cdn
{
    public class CdnHealth
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } = 0;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("avgTimeMs")]
        public int AvgTimeMs { get; set; } = 0;
    }
}
