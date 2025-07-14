using FmuApiDomain.Configuration;

namespace ApplicationConfigurationService.Migrations
{
    class MigrationTo10_2
    {
        public static Parameters DoMigration(Parameters settings)
        {
            if (settings.Database.NetAddress.Length == 0)
            {
                if (!string.IsNullOrEmpty(settings.Database.NetAdres))
                {
                    settings.Database.NetAddress = settings.Database.NetAdres;
                    settings.Database.NetAdres = null;
                }
            }
            else
            {
                settings.Database.NetAdres = null;
            }

            if (settings.Database.NetAddress.Length > 0 && settings.Database.UserName.Length > 0 && settings.Database.Password.Length > 0)
                settings.Database.Enable = true;

            settings.Database.FrontolDocumentsDbName = string.Empty;
            settings.Database.AlcoStampsDbName = string.Empty;

            settings.AppVersion = 10;
            settings.Assembly = 2;

            return settings;

        }
    }
}
