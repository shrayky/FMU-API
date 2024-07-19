using CouchDB.Driver.Types;
using FmuApiDomain.Models.MarkInformation;
using FmuApiDomain.Models.TrueSignApi.MarkData;

namespace FmuApiCouhDb.DocumentModels
{
    public class MarkStateDocument : CouchDocument
    {
        public string State { get; set; } = string.Empty;
        public CodeDataTrueApi TrueApiInformation { get; set; } = new();
        public TrueApiAnswerProperties TrueApiAnswerProperties { get; set; } = new();
        public SaleData SaleInforamtion { get; set; } = new();
    }
}
