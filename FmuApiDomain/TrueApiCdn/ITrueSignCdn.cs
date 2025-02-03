namespace FmuApiDomain.TrueApiCdn
{
    public interface ITrueSignCdn
    {
        public void BringOffline();
        public void BringOnline();

        public bool Offline();
        public DateTime BeginOfflineDate(); 
    }
}
