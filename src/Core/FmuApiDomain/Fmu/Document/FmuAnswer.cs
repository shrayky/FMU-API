// Ignore Spelling: Respinse Fmu

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
        public List<Organization> Organizations { get; set; } = [];
        [JsonPropertyName("fmu-api-offline")]
        public bool Offline { get; set; } = false;
        [JsonPropertyName("fmu-api-localModul")]
        public bool OfflineRegime { get; set; } = false;
        [JsonPropertyName("fmu-api-printgroup")]
        public int PrintGroupCode { get; set; } = 0;
        [JsonPropertyName("truemark_response")]
        public CheckMarksDataTrueApi Truemark_response { get; set; } = new();
        [JsonPropertyName("truemark_responses")]
        public List<CheckResult> TrueMarkResponses { get; set; } = [];
        [JsonPropertyName("offline_truemark_response")]
        public List<CheckMarksDataTrueApi> OffLineTrueMarkResponses { get; set; } = [];
        [JsonPropertyName("dmdk_responses")]
        public List<CheckMarksDataTrueApi> DmdkResponses { get; set; } = [];

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

        public FmuAnswer()
        {
        }

    }
}
