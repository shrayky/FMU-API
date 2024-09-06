using FmuApiDomain.Models.TrueSignApi.MarkData.Check;

namespace FmuApiDomain.Models.Fmu.Document
{
    public class FmuAnswer

    {
        public int Code { get; set; } = 0;
        public string Error { get; set; } = string.Empty;
        public List<string> Stamps { get; set; } = [];
        public List<string> Marking_codes { get; set; } = [];
        public List<Organization> Organizations { get; set; } = [];
        public CheckMarksDataTrueApi Truemark_response { get; set; } = new();
        
        public bool IsEmpty => (Stamps.Count == 0 && Marking_codes.Count == 0);
        public bool AllMarksIsSold() => Truemark_response.AllMarksIsSold();
        public bool AllMarksIsExpire() => Truemark_response.AllMarksIsExpire();

        public string SGtin()
        {
            return Truemark_response.SGtin();
        }

        public FmuAnswer()
        {
        }
     
    }
}
