using FmuApiDomain.Models.TrueSignApi.MarkData;

namespace FmuApiDomain.Models.MarkInformation
{
    public class MarkInformation
    {
        public string MarkId { get; set; } = string.Empty;
        public string State { get; set; } = MarkState.Stock;
        public CodeDataTrueApi TrueApiCisData { get; set; } = new();
        public TrueApiAnswerProperties TrueApiAnswerProperties { get; set; } = new();
        public SaleData SaleData { get; set; } = new();
        public bool HaveTrueApiAnswer => TrueApiAnswerProperties.ReqId != string.Empty;

        public bool IsSold => State == MarkState.Sold;
    }
}
