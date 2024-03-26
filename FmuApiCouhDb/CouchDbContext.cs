using CouchDB.Driver;
using CouchDB.Driver.Options;
using FmuApiCouhDb.DocumentModels;
using FmuApiSettings;

namespace FmuApiCouhDb
{
    public class CouchDbContext : CouchContext
    {
        private string _databaseName = Constants.Parametrs.MarksDb.DatabaseName;
        public CouchDatabase<MarkStateDocument> MarkState { get; set; }

        public CouchDbContext(CouchOptions<CouchDbContext> options) : base(options)
        {
            _databaseName = Constants.Parametrs.MarksDb.DatabaseName.ToLower();
        }

        public CouchDbContext(CouchOptions<CouchDbContext> options, string dbName) : base(options)
        {
            _databaseName = dbName.ToLower();
        }

        protected override void OnDatabaseCreating(CouchDatabaseBuilder databaseBuilder)
        {
            databaseBuilder.Document<MarkStateDocument>().ToDatabase(_databaseName == string.Empty ? "engels" : _databaseName);
        }
    }
}