namespace FmuApiDomain.Models
{
    public class SaleControlConfig
    {
        public bool BanSalesReturnedWares { get; set; } = false;
        public string IgnoreVerificationErrorForTrueApiGroups { get; set; } = string.Empty;
    }
}
