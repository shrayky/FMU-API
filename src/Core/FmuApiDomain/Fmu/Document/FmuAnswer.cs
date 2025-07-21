// Ignore Spelling: Respinse Fmu

using FmuApiDomain.Constants;
using FmuApiDomain.Fmu.MarkCheckData;
using FmuApiDomain.TrueApi.MarkData.Check;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.Document
{
    public class FmuAnswer

    {
        public int Code { get; set; } = 0;
        public string Error { get; set; } = string.Empty;
        public List<string> Stamps { get; set; } = [];
        public List<string> Marking_codes { get; set; } = [];
        [JsonPropertyName("truemark_response")]
        public CheckMarksDataTrueApi Truemark_response { get; set; } = new();
        [JsonPropertyName("truemark_responses")]
        public List<CheckResult> TrueMarkResponses { get; set; } = [];
        [JsonPropertyName("offline_truemark_response")]
        public List<CheckMarksDataTrueApi> OffLineTrueMarkResponses { get; set; } = [];
        [JsonPropertyName("dmdk_responses")]
        public List<CheckMarksDataTrueApi> DmdkResponses { get; set; } = [];
        [JsonPropertyName("fmu-api-offline")]
        public bool Offline { get; set; } = false;
        [JsonPropertyName("fmu-api-local-Module")]
        public bool OfflineRegime { get; set; } = false;
        [JsonPropertyName("fmu-api-print-group")]
        public int PrintGroupCode { get; set; } = 0;
        [JsonPropertyName("fmu-api-version")]
        public string FmuApiVersion {  get; set; } = $"{ApplicationInformation.AppVersion}.{ApplicationInformation.Assembly}";
        [JsonIgnore]
        public bool IsEmpty => Stamps.Count == 0 && Marking_codes.Count == 0;
        public bool AllMarksIsSold() => Truemark_response.AllMarksIsSold();
        public bool AllMarksIsExpire() => Truemark_response.AllMarksIsExpire();

        public string SGtin()
        {
            return Truemark_response.SGtin();
        }

        public void FillFieldsFor6255()
        {
            CheckInformation checkInformation = new();
            checkInformation.Codes = Truemark_response.Codes;

            CheckResult checkResult = new()
            {
                Inn = "",
                Kpp = "",
                Response = checkInformation
            };
            TrueMarkResponses.Add(checkResult);
        }

        public void FillFieldsFor6255(string Inn)
        {
            CheckInformation checkInformation = new();
            checkInformation.Codes = Truemark_response.Codes;

            CheckResult checkResult = new()
            {
                Inn = Inn,
                Kpp = "",
                Response = checkInformation
            };

            checkInformation.Description = Truemark_response.Description;
            checkInformation.ReqId = Truemark_response.ReqId;
            checkInformation.ReqTimestamp = Truemark_response.ReqTimestamp;
            checkInformation.Inst = Truemark_response.Inst;
            checkInformation.Version = Truemark_response.Version;

            TrueMarkResponses.Add(checkResult);
            
        }

        public FmuAnswer()
        {
        }

    }
}
