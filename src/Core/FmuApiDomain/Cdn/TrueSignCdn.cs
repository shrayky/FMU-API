namespace FmuApiDomain.Cdn
{
    public class TrueSignCdn
    {
        public string Host { get; set; } = string.Empty;
        public int Latency { get; set; } = 0;
        public bool Offline { get; private set; } = false;
        public DateTime OfflineFrom { get; private set; } = DateTime.MinValue;

        public void BringOffline()
        {
            Offline = true;
            OfflineFrom = DateTime.Now;
        }

        public void BringOnline()
        {
            Offline = false;
            OfflineFrom = DateTime.MinValue;
        }
    }
}
