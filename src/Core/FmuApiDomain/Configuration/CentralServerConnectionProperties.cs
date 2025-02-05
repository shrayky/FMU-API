namespace FmuApiDomain.Configuration
{
    public class CentralServerConnectionProperties
    {
        public bool Enabled { get; set; } = false;
        public string Address {  get; set; } = string.Empty;
        public string Token {  get; set; } = string.Empty;
        public int ExchangeRequestInterval { get; set; } = 600;
    }
}
