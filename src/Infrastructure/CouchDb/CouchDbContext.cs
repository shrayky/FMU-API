// Ignore Spelling: Frontol

using CouchDb.DatabaseScheme;
using CouchDb.DocumentModels;
using CouchDb.Documents;
using CouchDB.Driver;
using CouchDB.Driver.Options;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.MarkInformation.Entities;

namespace CouchDb
{
    public class CouchDbContext : CouchContext
    {
        public CouchDatabase<CouchDoc<MarkEntity>> Marks { get; set; }
        public CouchDatabase<CouchDoc<DocumentEntity>> Documents { get; set; }
        public CouchDatabase<CouchDoc<StatisticEntity>> MarkCheckingStatistic { get; set; }
        public CouchDatabase<CouchDoc<BeerTapEntity>> BeerOnTap { get; set; }
        public CouchDatabase<CouchDoc<GisMtDocumentEntity>> GisMtDocuments { get; set; }
        public CouchDatabase<CouchDoc<GisMtMarkEntity>> GisMtMarks { get; set; }

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

            databaseBuilder.Document<CouchDoc<BeerTapEntity>>().ToDatabase(DatabaseNames.BeerOnTaps);

            databaseBuilder.Document<CouchDoc<GisMtDocumentEntity>>().ToDatabase(DatabaseNames.GisMtDocumentsDbName);

            databaseBuilder.Document<CouchDoc<GisMtMarkEntity>>().ToDatabase(DatabaseNames.GisMtMarksDbName);
        }
    }
}