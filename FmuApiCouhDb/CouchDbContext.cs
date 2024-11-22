using CouchDb.DocumentModels;
using CouchDB.Driver;
using CouchDB.Driver.Options;
using FmuApiSettings;

namespace CouchDb
{
    public class CouchDbContext : CouchContext
    {
        private string _markDbName = Constants.Parametrs.Database.MarksStateDbName;
        private string _frontolDoumentsDbName = Constants.Parametrs.Database.FrontolDocumentsDbName;
        private string _alcoStampsDbName = Constants.Parametrs.Database.AlcoStampsDbName;

        public CouchDatabase<MarkStateDocument> MarksState { get; set; }
        public CouchDatabase<FrontolDocumentData> FrontolDocuments { get; set; }

        public CouchDbContext(CouchOptions<CouchDbContext> options) : base(options)
        {
            if (Constants.Parametrs.Database.MarksStateDbName != string.Empty)
                _markDbName = Constants.Parametrs.Database.MarksStateDbName.ToLower();

            if (Constants.Parametrs.Database.FrontolDocumentsDbName != string.Empty)
                _frontolDoumentsDbName = Constants.Parametrs.Database.FrontolDocumentsDbName.ToLower();

            if (Constants.Parametrs.Database.AlcoStampsDbName != string.Empty)
                _alcoStampsDbName = Constants.Parametrs.Database.AlcoStampsDbName.ToLower();
        }

        protected override void OnConfiguring(CouchOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrEmpty(Constants.Parametrs.Database.NetAdres))
            {
                optionsBuilder
                    .UseEndpoint("http://localhost:5984")
                    .UseBasicAuthentication("nouser", "nopassword");
            }
            else
            {
                optionsBuilder
                    .UseEndpoint(Constants.Parametrs.Database.NetAdres)
                    .EnsureDatabaseExists()
                    .UseBasicAuthentication(Constants.Parametrs.Database.UserName, Constants.Parametrs.Database.Password);
            }
        }

        protected override void OnDatabaseCreating(CouchDatabaseBuilder databaseBuilder)
        {
            databaseBuilder.Document<MarkStateDocument>().ToDatabase(_markDbName == string.Empty ? "engels" : _markDbName);
            databaseBuilder.Document<FrontolDocumentData>().ToDatabase(_frontolDoumentsDbName == string.Empty ? "frontoldocs" : _frontolDoumentsDbName);
        }
    }
}