using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.Configuration
{
    public class AlcoUnitConfig
    {
        public string NetAdres { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        [JsonIgnore]
        public string Token { get; set; } = string.Empty;

    }
}
