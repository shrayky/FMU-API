using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.TrueSignApi.Cdn
{
    public class CdnHealth
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } = 0;
        [JsonPropertyName("description")]
        public string Desciption { get; set; } = string.Empty;
        [JsonPropertyName("avgTimeMs")]
        public int AvgTimeMs { get; set; } = 0;
    }
}
