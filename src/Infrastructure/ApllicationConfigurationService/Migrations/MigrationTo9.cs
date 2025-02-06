using ApplicationConfigurationService.Settings;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Configuration.Options.Organisation;

namespace ApplicationConfigurationService.Migrations
{
    public class MigrationTo9
    {
        public static Parameters DoMigration(Parameters settings)
        {
            settings.HostsToPing = FillHostsToPing(settings.HostToPing);

            if (settings.OrganisationConfig.PrintGroups.Count == 0)
                settings.OrganisationConfig = MoveXapiConfiguration(settings.XAPIKEY);

            if (settings.NodeName == string.Empty)
                settings.NodeName = Environment.MachineName;

            settings.AppVersion = 9;

            return settings;
        }

        private static OrganizationConfiguration MoveXapiConfiguration(string? XAPIKEY)
        {
            if (XAPIKEY == null)
                return new();

            OrganizationConfiguration answer = new();

            PrintGroupData xapi = new()
            {
                Id = 1,
                XAPIKEY = XAPIKEY,
            };

            answer.PrintGroups.Add(xapi);

            return answer;
        }

        private static List<StringValue> FillHostsToPing(string? hostToPing)
        {
            if (hostToPing == null)
                return [];

            if (hostToPing == string.Empty)
                return [];

            var hosts = new List<string>();

            if (hostToPing == "https://mail.ru")
            {
                hosts = DefaultHostsToPing.Hosts();
            }
            else
                hosts.Add(hostToPing);

            List<StringValue> answer = [];

            int i = 1;

            foreach (var host in hosts)
            {
                answer.Add(new()
                {
                    Id = i,
                    Value = host
                });

                i++;
            }

            return answer;
        }
    }
}
