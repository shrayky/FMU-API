using FmuApiDomain.Attributes;

namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Общие настройки интеграции с ГИС МТ (не привязаны к организации).
/// </summary>
public record GisMtSettings
{
    [ExchangeableSetting]
    public int MtDocumentsPollIntervalMinutes { get; set; } = 10;

    [ExchangeableSetting]
    public int MarkRetentionDays { get; set; } = 365;

    [ExchangeableSetting]
    public int DocumentsSyncDays { get; set; } = 1;

    [ExchangeableSetting]
    public bool StockLoadEnabled { get; set; }

    [ExchangeableSetting]
    public TimeOnly StockLoadTime { get; set; } = new(3, 0);

    public bool DocumentsEnabled { get; set; }

    public bool AutoCreateConnectTap { get; set; }

    public bool AutoSendConnectTap { get; set; }
}
