namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Настройки хранения статистики проверок марок.
/// </summary>
public class StatisticsSettings
{
    public bool SaveToDb { get; set; } = true;

    public bool ClearStorageOfStatistics { get; set; } = true;

    public int DepthOfStorageOfStatisticsInDays { get; set; } = 30;
}
