using FmuApiDomain.Configuration;
using FmuApiDomain.TrueApi.MarkData;

namespace ApplicationConfigurationService.Migrations;

/// <summary>
/// Включает проверку равенства цены МРЦ для уже сохранённых групп табака.
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

        settings.AppVersion = 12;
        settings.Assembly = 2;

        return settings;
    }
}
