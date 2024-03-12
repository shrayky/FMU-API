using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.TrueSignTokenService
{
    public class TokenData
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;
        [JsonPropertyName("until")]
        public DateTime Expired { get; set; } = DateTime.MaxValue;
    }
}
