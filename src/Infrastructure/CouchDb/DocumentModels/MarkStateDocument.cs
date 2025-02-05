using CouchDB.Driver.Types;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.TrueApi.MarkData;

namespace CouchDb.DocumentModels
{
    public class MarkStateDocument : CouchDocument
    {
        public string State { get; set; } = string.Empty;
        public CodeDataTrueApi TrueApiInformation { get; set; } = new();
        public TrueApiAnswerData TrueApiAnswerProperties { get; set; } = new();
        public SaleData SaleInforamtion { get; set; } = new();
    }
}
