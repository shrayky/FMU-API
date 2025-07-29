// Ignore Spelling: Frontol

using CouchDb.DocumentModels;
using CouchDb.Documents;
using CouchDB.Driver;
using CouchDB.Driver.Options;
using FmuApiDomain.Frontol;
using FmuApiDomain.MarkInformation.Entities;

namespace CouchDb
{
    public class CouchDbContext : CouchContext
    {
        public CouchDatabase<CouchDoc<MarkEntity>> Marks { get; set; }
        public CouchDatabase<CouchDoc<DocumentEntity>> Documents { get; set; }
        
        // устаревшие:
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
            databaseBuilder.Document<CouchDoc<MarkEntity>>().ToDatabase(DatabaseNames.MarksDbName);
            databaseBuilder.Document<CouchDoc<DocumentEntity>>().ToDatabase(DatabaseNames.DocumentsDbName);

            // устаревшие базы, для совместимости:
            Сompatibility9_102(databaseBuilder);
        }

        private void Сompatibility9_102(CouchDatabaseBuilder databaseBuilder)
        {
            string _markStateDbName = DatabaseNames.MarksStateDb;
            string _frontolDocumentsDbName = DatabaseNames.FrontolDocumentsDb;

            if (!string.IsNullOrEmpty(_markStateDbName))
                databaseBuilder.Document<MarkStateDocument>().ToDatabase(_markStateDbName);

            if (!string.IsNullOrEmpty(_frontolDocumentsDbName))
                databaseBuilder.Document<FrontolDocumentData>().ToDatabase(_frontolDocumentsDbName);
        }
    }
}