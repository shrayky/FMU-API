using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration.Options
{
    public class AutoUpdateOptions
    {
        [JsonInclude]
        public bool Enabled { get; private set; } = false;
        [JsonInclude]
        public string UpdateFilesCatalog { get; private set; } = string.Empty;
        [JsonInclude]
        public int FromHour { get; private set; } = 0;
        [JsonInclude]
        private int UntilHour { get; set; } = 0;

        [JsonConstructor]
        private AutoUpdateOptions()
        {
        }

        public static AutoUpdateOptions Create() => new();

        public int CanUpdateUntil() => UntilHour == 0 ? 24 : UntilHour;
        
    }
}
