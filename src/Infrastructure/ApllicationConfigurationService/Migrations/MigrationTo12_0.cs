using FmuApiDomain.Configuration;

namespace ApplicationConfigurationService.Migrations;

public class MigrationTo12_0
{
    /// <summary>
    /// Переносит параметры хранения статистики из настроек CouchDB в отдельную секцию Statistics.
    /// </summary>
    public static Parameters DoMigration(Parameters settings)
    {
#pragma warning disable CS0612
        if (settings.Database.ClearStorageOfStatistics.HasValue)
            settings.Statistics.ClearStorageOfStatistics = settings.Database.ClearStorageOfStatistics.Value;

        if (settings.Database.DepthOfStorageOfStatisticsInDays.HasValue)
            settings.Statistics.DepthOfStorageOfStatisticsInDays = settings.Database.DepthOfStorageOfStatisticsInDays.Value;

        settings.Database.ClearStorageOfStatistics = null;
        settings.Database.DepthOfStorageOfStatisticsInDays = null;
#pragma warning restore CS0612

        settings.AppVersion = 12;
        settings.Assembly = 0;

        return settings;
    }
}
