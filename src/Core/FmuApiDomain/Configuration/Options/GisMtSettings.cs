namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Общие настройки интеграции с ГИС МТ (не привязаны к организации).
/// </summary>
public record GisMtSettings
{
    /// <summary>
    /// Интервал опроса входящих документов ГИС МТ (минуты).
    /// </summary>
    public int MtDocumentsPollIntervalMinutes { get; set; } = 10;

    /// <summary>
    /// Срок хранения невалидных марок остатка (дни) до удаления.
    /// </summary>
    public int MarkRetentionDays { get; set; } = 365;

    /// <summary>
    /// Количество календарных дней для загрузки документов (включая текущий день).
    /// </summary>
    public int DocumentsSyncDays { get; set; } = 1;

    /// <summary>
    /// Автоматическая ежедневная загрузка остатков марок.
    /// </summary>
    public bool StockLoadEnabled { get; set; }

    /// <summary>
    /// Время ежедневной загрузки остатков (локальное время сервера).
    /// </summary>
    public TimeOnly StockLoadTime { get; set; } = new(3, 0);
}
