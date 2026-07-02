namespace FmuApiDomain.Configuration.Options;

public class SaleControlConfig
{
    public bool BanSalesReturnedWares { get; set; } = false;

    public string IgnoreVerificationErrorForTrueApiGroups { get; set; } = string.Empty;

    public bool CheckReceiptReturn { get; set; } = false;

    public bool CorrectExpireDateInSaleReturn { get; set; } = false;

    public bool CheckIsOwnerField { get; set; } = false;

    public bool SendLocalModuleInformationalInRequestId { get; set; } = true;

    public bool ResetSoldStatusForReturn { get; set; } = false;

    public bool UseBeerTaps { get; set; } = false;

    public MarkCheckResultSave MarkCheckResultSave { get; set; } = new();

    [Obsolete]
    public bool? SendEmptyTrueApiAnswerWhenTimeoutError { get; set; } = false;

    [Obsolete]
    public DateTime? RejectSalesWithoutCheckInformationFrom { get; set; }
}

// для дотарифных фронтолов - сохранение результатов проверки марки для подставновки в драйвер атол через скрипты
public record MarkCheckResultSave
{
    public bool Enable { get; set; } = false;
    public string Directory { get; set; } = string.Empty;
    public int FileLifespanHours { get; set; } = 1;
}
