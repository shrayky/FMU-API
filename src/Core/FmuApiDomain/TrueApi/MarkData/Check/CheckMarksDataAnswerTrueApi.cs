using System.Text.Json.Serialization;

namespace FmuApiDomain.TrueApi.MarkData.Check
{
    public class CheckMarksDataTrueApi
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("reqId")]
        public string ReqId { get; set; } = string.Empty;
        
        [JsonPropertyName("reqTimestamp")]
        public long ReqTimestamp { get; set; }
        
        [JsonPropertyName("inst")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Inst { get; set; } = "";
        
        [JsonPropertyName("version")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Version { get; set; } = "";
        
        [JsonPropertyName("codes")]
        public List<CodeDataTrueApi> Codes { get; set; } = [];

        public bool AllMarksIsSold()
        {
            var sold = 0;

            foreach (var markData in Codes)
            {
                if (markData.Sold)
                    sold++;
            }

            return sold == Codes.Count;
        }

        public bool AllMarkIsNotRealizable() => Codes.Count(p => !p.Realizable) == Codes.Count;

        public bool AllMarksIsExpire()
        {
            var expire = 0;

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

        public string SGtin()
        {
            if (Codes.Count != 1)
                return "";

            var code = Codes[0].Cis;

            if (code.StartsWith("01"))
                code = $"{code.Substring(2, 14)}{code.Substring(18)}";

            return code;
        }

        public CodeDataTrueApi MarkData()
        {
            if (Codes.Count != 1)
                return new CodeDataTrueApi();

            return Codes[0];
        }

        public void CorrectExpireDate()
        {
            foreach (var data in Codes)
            {
                if (data.ExpireDate == null)
                    continue;

                if (data.ExpireDate < DateTime.Now)
                    data.ExpireDate = DateTime.Now.AddDays(1);

            }
        }
        public void ResetErrorFields(bool resetSoldStatusForReturn = false)
        {
            foreach (var data in Codes)
                data.ResetErrorFields(resetSoldStatusForReturn);
        }

    }
}
