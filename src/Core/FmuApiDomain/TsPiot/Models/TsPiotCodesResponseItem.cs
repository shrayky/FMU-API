using FmuApiDomain.TrueApi.MarkData;
using System.Text.Json.Serialization;

namespace FmuApiDomain.TsPiot.Models
{
    public record TsPiotCodesResponseItem
    {
        [JsonPropertyName("codes")]
        public List<CodeDataTrueApi> Codes { get; set; } = [];

        [JsonPropertyName("reqId")]
        public string RequestId { get; set; } = string.Empty;

        [JsonPropertyName("reqTimestamp")]
        public long RequestTimestamp { get; set; }

        [JsonPropertyName("isCheckedOffline")]
        public bool IsCheckedOffline { get; set; }

        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string LocalModuleVersion { get; set; } = string.Empty;

        [JsonPropertyName("inst")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string LocalModuleInstance { get; set; } = string.Empty;
        
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
