using System.Text.Json.Serialization;

namespace FmuApiDomain.Models.Fmu.Token
{
    public class AuthorizationAnswer
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("role")]
        public string Role { get; set; } = "pos";
        [JsonPropertyName("expired")]
        public int Expired { get; set; } = 0;
        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;
    }
}
