using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Constants;

namespace ApplicationConfigurationService.Migrations;

public class MigrationTo11_10
{
    public static Parameters DoMigration(Parameters settings)
    {
        if (settings.FrontolConnectionSettings != null)
            settings = MoveFrontolConnectionSettingsToConnectedFrontolSettings(settings);

        settings.AppVersion = ApplicationInformation.AppVersion;
        settings.Assembly = ApplicationInformation.Assembly;

        return settings;
    }

    private static Parameters MoveFrontolConnectionSettingsToConnectedFrontolSettings(Parameters settings)
    {
        if (settings.ConnectedFrontolSettings.ConnectionSettings.Count > 0)
            return settings;

        if (settings.FrontolConnectionSettings.ConnectionStringBuild() == string.Empty)
        {
            settings.FrontolConnectionSettings = new();
            return settings;
        }

        var conn = new FrontolConnectionSettings()
        {
            Id = 1,
            Name = "Default",
            Path = settings.FrontolConnectionSettings.Path,
            UserName = settings.FrontolConnectionSettings.UserName,
            Password = settings.FrontolConnectionSettings.Password
        };

        settings.ConnectedFrontolSettings.ConnectionSettings.Add(conn);
        settings.ConnectedFrontolSettings.PrintGroupSourseId = 1;
        settings.FrontolConnectionSettings = new();

        return settings;
    }
}
