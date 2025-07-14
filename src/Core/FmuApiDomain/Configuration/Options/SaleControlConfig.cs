namespace FmuApiDomain.Configuration.Options
{
    public class SaleControlConfig
    {
        public bool BanSalesReturnedWares { get; set; } = false;
        public string IgnoreVerificationErrorForTrueApiGroups { get; set; } = string.Empty;
        public bool CheckReceiptReturn { get; set; } = false;
        public bool CorrectExpireDateInSaleReturn { get; set; } = false;
        public bool SendEmptyTrueApiAnswerWhenTimeoutError { get; set; } = false;
        public bool CheckIsOwnerField { get; set; } = false;
        public bool SendLocalModuleInformationalInRequestId { get; set; } = false;
        public DateTime RejectSalesWithoutCheckInformationFrom { get; set; } = new DateTime(2025, 3, 1);
        public bool ResetSoldStatusForReturn { get; set; } = false;
    }
}
