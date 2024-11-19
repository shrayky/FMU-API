using CouchDB.Driver.Types;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;

namespace FmuApiCouhDb.DocumentModels
{
    public class FrontolDocumentData: CouchDocument, IFrontolDocumentData
    {
        public RequestDocument Document { get; set; } = new();
    }
}
