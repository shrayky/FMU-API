namespace FmuApiDomain.Models.Configuration
{
    public class SaleControlConfig
    {
        public bool BanSalesReturnedWares { get; set; } = false;
        public string IgnoreVerificationErrorForTrueApiGroups { get; set; } = string.Empty;
        public bool CheckReceiptReturn { get; set; } = false;
    }
}
