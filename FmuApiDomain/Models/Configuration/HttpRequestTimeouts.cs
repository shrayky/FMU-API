namespace FmuApiDomain.Models.Configuration
{
    public class HttpRequestTimeouts
    {
        public int CdnRequestTimeout { get; set; } = 15;
        public int CheckMarkRequestTimeout { get; set; } = 2;
        public int CheckInternetConnectionTimeout { get; set; } = 15;
    }
}
