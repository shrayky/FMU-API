using FmuApiDomain.Configuration;

namespace ApplicationConfigurationService.Migrations;

public class MigrationTo12_1
{
    /// <summary>
    /// Переносит версию локального модуля из ServerConfig.LocalModuleVersion в ServerConfig.LocalModuleGeneral.
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

        settings.AppVersion = 12;
        settings.Assembly = 1;

        return settings;
    }
}
