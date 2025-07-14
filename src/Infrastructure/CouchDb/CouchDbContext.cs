using CouchDb.DocumentModels;
using CouchDB.Driver;
using CouchDB.Driver.Options;

namespace CouchDb
{
    public class CouchDbContext : CouchContext
    {
        public CouchDatabase<MarkStateDocument> MarksState { get; set; }
        public CouchDatabase<FrontolDocumentData> FrontolDocuments { get; set; }

        public CouchDbContext(CouchOptions<CouchDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(CouchOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnDatabaseCreating(CouchDatabaseBuilder databaseBuilder)
        {
            string _markStateDbName = DatabaseNames.MarksStateDb;
            string _frontolDocumentsDbName = DatabaseNames.FrontolDocumentsDb;

            databaseBuilder.Document<MarkStateDocument>().ToDatabase(_markStateDbName == string.Empty ? "fmu-marks" : _markStateDbName);
            databaseBuilder.Document<FrontolDocumentData>().ToDatabase(_frontolDocumentsDbName == string.Empty ? "fmu-cashdocs" : _frontolDocumentsDbName);
        }
    }
}