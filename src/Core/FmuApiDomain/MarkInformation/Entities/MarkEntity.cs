using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Templates.Tables;
using FmuApiDomain.TrueApi.MarkData;

namespace FmuApiDomain.MarkInformation.Entities
{
    public class MarkEntity: IHaveStringId
    {
        public string Id { get; set; } = string.Empty;
        public string MarkId { get; set; } = string.Empty;
        public string State { get; set; } = MarkState.Stock;
        public CodeDataTrueApi TrueApiCisData { get; set; } = new();
        public TrueApiAnswerData TrueApiAnswerProperties { get; set; } = new();
        public SaleData SaleData { get; set; } = new();
        public List<SaleData> Sales { get; set; } = new();
        public bool HaveTrueApiAnswer => TrueApiAnswerProperties.ReqId != string.Empty;
        public bool IsSold => State == MarkState.Sold;

        public static MarkEntity Create(string sGtin, CodeDataTrueApi markCodeData, MarkEntity existMark, TrueApiAnswerData trueApiAnswerData)
        {
            string state = string.IsNullOrEmpty(existMark.State) ? MarkState.Stock : existMark.State;

            var entity = new MarkEntity() 
            {
                Id = string.IsNullOrEmpty(existMark.Id) ? sGtin : existMark.Id,
                MarkId = sGtin,
                State = markCodeData.Sold ? MarkState.Sold : state,
                TrueApiCisData = markCodeData,
                TrueApiAnswerProperties = trueApiAnswerData,
                Sales = existMark.Sales,
                SaleData = existMark.SaleData
            };

            return entity;
        }
    }
}
