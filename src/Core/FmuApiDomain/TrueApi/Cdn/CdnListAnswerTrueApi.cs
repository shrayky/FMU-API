using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.Cdn
{
    public class CdnListAnswerTrueApi
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } = 0;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("hosts")]
        public List<CdnHost> Hosts { get; set; } = [];
    }
}
