using FmuApiDomain.Models.TrueSignApi.MarkData;

namespace FmuApiDomain.Models.MarkState
{
    public class MarkState
    {
        public string MarkId { get; set; } = string.Empty;
        public string State {  get; set; } = string.Empty;
        public CodeDataTrueApi TrueApiCisData { get; set; } = new();
        public TrueApiAnswerProperties TrueApiAnswerProperties {  get; set; } = new();
        public SaleData SaleData { get; set; } = new();

    }
}
