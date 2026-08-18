using FmuApiDomain.Configuration;
using FmuApiDomain.TrueApi.MarkData;

namespace ApplicationConfigurationService.Migrations;

public class MigrationTo12_1
{
    /// <summary>
    /// Переносит версию локального модуля и заполняет маппинг Frontol → Честный знак.
    /// </summary>
    public static Parameters DoMigration(Parameters settings)
    {
#pragma warning disable CS0612
        if (settings.ServerConfig.LocalModuleVersion.HasValue)
        {
            settings.ServerConfig.LocalModuleGeneral.Version = settings.ServerConfig.LocalModuleVersion.Value;
            settings.ServerConfig.LocalModuleVersion = null;
        }
#pragma warning restore CS0612

        if (settings.GisMtProductMappings.Count == 0 || settings.Assembly > 1)
            settings.GisMtProductMappings = AtolToTrueApiGroupMap.CopyDefaults();

        settings.AppVersion = 12;
        settings.Assembly = 1;

        return settings;
    }
}
