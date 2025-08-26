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
            databaseBuilder.Document<CouchDoc<MarkEntity>>().ToDatabase(DatabaseNames.MarksDbName)
                .HasIndex("mark-id-idx", i => i.IndexBy(c => c.Data.MarkId))
                .HasIndex("mark-data-idx", i => i.IndexBy(c => c.Data))
                .HasIndex("timeStamp-data-idx", i => i.IndexBy(c => c.Data.TrueApiAnswerProperties.ReqTimestamp));
            databaseBuilder.Document<CouchDoc<DocumentEntity>>().ToDatabase(DatabaseNames.DocumentsDbName);
            databaseBuilder.Document<CouchDoc<StatisticEntity>>().ToDatabase(DatabaseNames.MarkCheckingStatistic)
                    .HasIndex("date-time-idx", p => p.IndexBy(c => c.Data.checkDate))
                    .HasIndex("date-sgtin", p => p.IndexBy(c => c.Data.SGtin));

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