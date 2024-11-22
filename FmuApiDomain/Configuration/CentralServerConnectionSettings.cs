namespace FmuApiDomain.Configuration
{
    public class CentralServerConnectionSettings
    {
        public bool Enabled { get; set; } = false;
        public string Adres {  get; set; } = string.Empty;
        public string Token {  get; set; } = string.Empty;
        public int ExchangeRequestInterval { get; set; } = 600;
    }
}
