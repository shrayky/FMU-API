
namespace FmuApiDomain.Models.Configuration
{
    public class CouchDbConnection
    {
        public string NetAdres { get; set; } = "http://localhost:5984";
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string MarksStateDbName { get; set; } = string.Empty;
        public string FrontolDocumentsDbName {  get; set; } = string.Empty;
        public string AlcoStampsDbName { get; set; } = string.Empty;

        public bool ConfigurationIsEnabled => (NetAdres != string.Empty && UserName != string.Empty && Password != string.Empty);
        public bool OfflineCheckIsEnabled => (ConfigurationIsEnabled && MarksStateDbName.Length > 0);

        public void CheckDbNames()
        {
            MarksStateDbName = MarksStateDbName.ToLower();
            FrontolDocumentsDbName = FrontolDocumentsDbName.ToLower();
            AlcoStampsDbName = AlcoStampsDbName.ToLower();

            MarksStateDbName = MarksStateDbName.Replace(@".", "_");
            FrontolDocumentsDbName = FrontolDocumentsDbName.Replace(@".", "_");
            AlcoStampsDbName = AlcoStampsDbName.Replace(@".", "_");
        }
    }
}
