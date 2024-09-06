using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.TrueSignApi.MarkData.Check
{
    public class CheckMarksDataTrueApi
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

        public bool AllMarksIsSold()
        {
            int sold = 0;

            foreach (var markData in Codes)
            {
                if (markData.Sold)
                    sold++;
            }

            return sold == Codes.Count;
        }

        public bool AllMarksIsExpire()
        {
            int expire = 0;

            foreach (var markData in Codes)
            {
                if (markData.IsExpired)
                    expire++;
            }

            return expire == Codes.Count;
        }

        // если в кодах маркировки у нас только 1 код, то помечает его его как проданый
        public void MarkCodeAsSaled()
        {
            if (Codes.Count != 1)
                return;

            Codes[0].Sold = true;
        }

        public void MarkCodeAsNotSaled()
        {
            if (Codes.Count != 1)
                return;

            Codes[0].Sold = false;
        }
    }
}
