// Ignore Spelling: Frontol

using CouchDb.DocumentModels;
using CouchDb.Documents;
using CouchDB.Driver;
using CouchDB.Driver.Options;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.MarkInformation.Entities;

namespace CouchDb
{
    public class CouchDbContext : CouchContext
    {
        public CouchDatabase<CouchDoc<MarkEntity>> Marks { get; set; }
        public CouchDatabase<CouchDoc<DocumentEntity>> Documents { get; set; }
        public CouchDatabase<CouchDoc<StatisticEntity>> MarkCheckingStatistic {  get; set; }
        
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

            databaseBuilder.Document<CouchDoc<StatisticEntity>>().ToDatabase(DatabaseNames.MarkCheckingStatistic);

            // устаревшие базы, для совместимости:
            Сompatibility9_102(databaseBuilder);
        }

        private void Сompatibility9_102(CouchDatabaseBuilder databaseBuilder)
        {
            var _markStateDbName = DatabaseNames.MarksStateDb;
            var _frontolDocumentsDbName = DatabaseNames.FrontolDocumentsDb;

            if (!string.IsNullOrEmpty(_markStateDbName))
                databaseBuilder.Document<MarkStateDocument>().ToDatabase(_markStateDbName);

            if (!string.IsNullOrEmpty(_frontolDocumentsDbName))
                databaseBuilder.Document<FrontolDocumentData>().ToDatabase(_frontolDocumentsDbName);
        }
    }
}