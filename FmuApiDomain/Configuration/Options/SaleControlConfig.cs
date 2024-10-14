namespace FmuApiDomain.Configuration.Options
{
    public class SaleControlConfig
    {
        public bool BanSalesReturnedWares { get; set; } = false;
        public string IgnoreVerificationErrorForTrueApiGroups { get; set; } = string.Empty;
        public bool CheckReceiptReturn { get; set; } = false;
        public bool CorectExpireDateInSaleReturn { get; set; } = false;
    }
}
