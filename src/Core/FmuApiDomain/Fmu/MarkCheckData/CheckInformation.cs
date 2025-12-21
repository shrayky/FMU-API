using FmuApiDomain.TrueApi.MarkData;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.MarkCheckData
{
    public class CheckInformation
    {
        public int Code { get; set; } = 0;
        public List<CodeDataTrueApi> Codes { get; set; } = [];
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("reqId")]
        public string ReqId { get; set; } = string.Empty;
        
        [JsonPropertyName("reqTimestamp")]
        public long ReqTimestamp { get; set; } = 0;
        
        [JsonPropertyName("inst")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Inst { get; set; } = "";
        
        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Version { get; set; } = "";
    }
}
