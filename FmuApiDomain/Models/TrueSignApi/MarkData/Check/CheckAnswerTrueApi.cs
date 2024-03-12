using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.TrueSignApi.MarkData.Check
{
    public class CheckAnswerTrueApi
    {
        [JsonPropertyName("code")]
        public int Code { get; set; } = 0;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("reqId")]
        public string ReqId { get; set; } = string.Empty;
        [JsonPropertyName("reqTimestamp")]
        public long ReqTimestamp { get; set; } = 0;
        [JsonPropertyName("codes")]
        public List<CodeDataTrueApi> Codes { get; set; } = [];
    }
}
