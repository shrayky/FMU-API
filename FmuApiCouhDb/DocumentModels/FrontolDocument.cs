using CouchDB.Driver.Types;
using FmuApiDomain.Models.Fmu.Document;

namespace FmuApiCouhDb.DocumentModels
{
    public class FrontolDocumentData : CouchDocument
    {
        public RequestDocument Document { get; set; } = new();
    }
}
