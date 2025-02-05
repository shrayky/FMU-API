using CouchDb.DocumentModels;
using CouchDB.Driver;
using CouchDB.Driver.Options;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;

namespace CouchDb
{
    public class CouchDbContext : CouchContext
    {
        private readonly IParametersService _parametersService;
        private readonly Parameters _configuration;

        private string _markDbName = string.Empty;
        private string _frontolDocumentsDbName = string.Empty;
        private string _alcoStampsDbName = string.Empty;

        public CouchDatabase<MarkStateDocument> MarksState { get; set; }
        public CouchDatabase<FrontolDocumentData> FrontolDocuments { get; set; }

        public CouchDbContext(CouchOptions<CouchDbContext> options, IParametersService parametersService) : base(options)
        {
            _parametersService = parametersService;
            _configuration = parametersService.Current();

            if (_configuration.Database.MarksStateDbName != string.Empty)
                _markDbName = _configuration.Database.MarksStateDbName.ToLower();

            if (_configuration.Database.FrontolDocumentsDbName != string.Empty)
                _frontolDocumentsDbName = _configuration.Database.FrontolDocumentsDbName.ToLower();

            if (_configuration.Database.AlcoStampsDbName != string.Empty)
                _alcoStampsDbName = _configuration.Database.AlcoStampsDbName.ToLower();
        }

        protected override void OnConfiguring(CouchOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnDatabaseCreating(CouchDatabaseBuilder databaseBuilder)
        {
            databaseBuilder.Document<MarkStateDocument>().ToDatabase(_markDbName == string.Empty ? "fmu-marks" : _markDbName);
            databaseBuilder.Document<FrontolDocumentData>().ToDatabase(_frontolDocumentsDbName == string.Empty ? "fmu-cashdocs" : _frontolDocumentsDbName);
        }
    }
}