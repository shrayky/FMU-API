using FmuApiDomain.Models.TrueSignApi.MarkData.Check;

namespace FmuApiDomain.Models.Fmu.Document
{
    public class AnswerDocument
    {
        public int Code { get; set; } = 0;
        public string Error { get; set; } = string.Empty;
        public List<string> Stamps { get; set; } = [];
        public List<string> Marking_codes { get; set; } = [];
        public List<Organization> Organizations { get; set; } = [];
        public CheckAnswerTrueApi Truemark_response { get; set; } = new();
    }
}
