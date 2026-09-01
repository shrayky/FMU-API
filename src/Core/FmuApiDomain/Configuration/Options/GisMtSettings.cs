namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Общие настройки интеграции с ГИС МТ (не привязаны к организации).
/// </summary>
public record GisMtSettings
{
    public int MtDocumentsPollIntervalMinutes { get; set; } = 10;

    public int MarkRetentionDays { get; set; } = 365;

    public int DocumentsSyncDays { get; set; } = 1;

    public bool StockLoadEnabled { get; set; }

    public TimeOnly StockLoadTime { get; set; } = new(3, 0);
}
