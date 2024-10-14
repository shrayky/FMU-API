namespace FmuApiDomain.Configuration.Options
{
    public class SignData
    {
        public string Signature { get; set; } = string.Empty;
        public DateTime Expired { get; set; } = DateTime.UnixEpoch;

        public string Token()
        {
            if (!string.IsNullOrEmpty(Signature) & Expired > DateTime.Now)
                return Signature;

            return "";
        }
    }
}
