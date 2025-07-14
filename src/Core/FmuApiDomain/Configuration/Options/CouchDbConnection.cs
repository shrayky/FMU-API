
using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration.Options
{
    public class CouchDbConnection
    {
        public bool Enable { get; set; } = false;
        public string NetAddress { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string MarksStateDbName { get; set; } = string.Empty;
        public string FrontolDocumentsDbName { get; set; } = string.Empty;
        public string AlcoStampsDbName { get; set; } = string.Empty;

        [JsonIgnore]
        public bool ConfigurationIsEnabled => NetAddress != string.Empty && UserName != string.Empty && Password != string.Empty && Enable;
        [JsonIgnore]
        public bool DatabaseCheckIsEnabled => ConfigurationIsEnabled && MarksStateDbName.Length > 0 && Enable;

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
