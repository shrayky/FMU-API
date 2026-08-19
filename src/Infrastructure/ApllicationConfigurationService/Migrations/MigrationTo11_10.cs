using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options;

namespace ApplicationConfigurationService.Migrations;

public class MigrationTo11_10
{
    public static Parameters DoMigration(Parameters settings)
    {
#pragma warning disable CS0612 // Тип или член устарел
        if (settings.FrontolConnectionSettings != null)
            settings = MoveFrontolConnectionSettingsToConnectedFrontolSettings(settings);
#pragma warning restore CS0612 // Тип или член устарел
        settings.AppVersion = 11;
        settings.Assembly = 10;

        return settings;
    }

    private static Parameters MoveFrontolConnectionSettingsToConnectedFrontolSettings(Parameters settings)
    {
        if (settings.ConnectedFrontolSettings.ConnectionSettings.Count > 0)
            return settings;
#pragma warning disable CS0612 // Тип или член устарел

        var frontolSettings = settings.FrontolConnectionSettings;

        if (frontolSettings == null)
        {
            settings.FrontolConnectionSettings = new();
            return settings;
        }

        var conn = new FrontolConnectionSettings()
        {
            Id = 1,
            Name = "Default",
            Path = frontolSettings.Path,
            UserName = frontolSettings.UserName,
            Password = frontolSettings.Password
        };

        settings.ConnectedFrontolSettings.ConnectionSettings.Add(conn);
        settings.ConnectedFrontolSettings.PrintGroupSourseId = 1;
        settings.FrontolConnectionSettings = new();

#pragma warning restore CS0612 // Тип или член устарел

        return settings;
    }
}
