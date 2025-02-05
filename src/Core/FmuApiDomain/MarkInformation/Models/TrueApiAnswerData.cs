namespace FmuApiDomain.MarkInformation.Models
{
    public class TrueApiAnswerData
    {
        public int Code { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
        public string ReqId { get; set; } = string.Empty;
        public long ReqTimestamp { get; set; } = 0;
    }
}
