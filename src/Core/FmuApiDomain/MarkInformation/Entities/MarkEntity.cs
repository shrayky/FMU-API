using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.TrueApi.MarkData;

namespace FmuApiDomain.MarkInformation.Entities
{
    public class MarkEntity
    {
        public string MarkId { get; set; } = string.Empty;
        public string State { get; set; } = MarkState.Stock;
        public CodeDataTrueApi TrueApiCisData { get; set; } = new();
        public TrueApiAnswerData TrueApiAnswerProperties { get; set; } = new();
        public SaleData SaleData { get; set; } = new();
        public bool HaveTrueApiAnswer => TrueApiAnswerProperties.ReqId != string.Empty;
        public bool IsSold => State == MarkState.Sold;

        public static implicit operator List<object>(MarkEntity v)
        {
            throw new NotImplementedException();
        }
    }
}
