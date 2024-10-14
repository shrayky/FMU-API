using Microsoft.VisualBasic;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Fmu.Token
{
    public class AuthorizationData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        public override string ToString() => $"{{\"id\": \"{Id}\", \"password\":\"{Password}\"}}";

    }
}
