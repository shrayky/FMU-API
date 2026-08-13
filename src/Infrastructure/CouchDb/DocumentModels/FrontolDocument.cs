using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;

namespace CouchDb.DocumentModels
{
    public class FrontolDocumentData : IFrontolDocumentData
    {
        public string Id { get; set; } = string.Empty;
        public string Rev { get; set; } = string.Empty;
        public bool Deleted { get; set; }
        public RequestDocument Document { get; set; } = new();
    }
}
