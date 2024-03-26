namespace FmuApiDomain.Models.Configuration
{
    public class CouchDbConnection
    {
        public string NetAdres { get; set; } = "http://localhost:5984";
        public string DatabaseName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public bool ConfigurationEnabled() => (NetAdres != string.Empty && DatabaseName != string.Empty && UserName != string.Empty && Password != string.Empty);
    }
}
