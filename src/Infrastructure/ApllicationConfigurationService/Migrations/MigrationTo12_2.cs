using FmuApiDomain.Configuration;
using FmuApiDomain.TrueApi.MarkData;

namespace ApplicationConfigurationService.Migrations;

/// <summary>
/// Включает проверку МРЦ для табака и переносит id базы Frontol в FrontolWareDataSourceId.
/// </summary>
public class MigrationTo12_2
{
    public static Parameters DoMigration(Parameters settings)
    {
        foreach (var mapping in settings.GisMtProductMappings)
        {
            if (mapping.TrueApiGroupId == TrueApiGroup.Tobaco)
                mapping.CheckMrp = true;
        }

#pragma warning disable CS0618
        if (settings.ConnectedFrontolSettings.FrontolWareDataSourceId == 0
            && settings.ConnectedFrontolSettings.PrintGroupSourseId is > 0)
        {
            settings.ConnectedFrontolSettings.FrontolWareDataSourceId =
                settings.ConnectedFrontolSettings.PrintGroupSourseId.Value;
        }

        settings.ConnectedFrontolSettings.PrintGroupSourseId = null;
#pragma warning restore CS0618

        settings.AppVersion = 12;
        settings.Assembly = 2;

        return settings;
    }
}
