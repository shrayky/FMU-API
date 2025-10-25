namespace FmuApiDomNewDirectory1ain.Configuration
{
    public class CentralServerConnectionProperties
    {
        public bool Enabled { get; set; } = false;
        public string Address {  get; set; } = string.Empty;
        public string Token {  get; set; } = string.Empty;
        public int ExchangeRequestInterval { get; set; } = 60;
        public string Secret { get; set; } = string.Empty;
        public bool DownloadNewVersion { get; set; } = false;
    }
}
