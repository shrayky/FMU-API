using System.Text.Json.Serialization;

namespace FmuApiDomain.Authentication.Models
{
    public class TokenData
    {
        [JsonPropertyName("token")]
        public string Token { get; private set; } = string.Empty;
        [JsonPropertyName("until")]
        public DateTime? Until { get; private set; }
        [JsonPropertyName("expired")]
        public DateTime? Expired { get; private set; }

        public TokenData() { }

        public TokenData(string token, DateTime expiresAt)
        {
            Token = token;
            Until = expiresAt;
            Expired = expiresAt;
        }
        public bool IsValid()
        {
            var expiresAt = ExpirationDate();
            return !string.IsNullOrEmpty(Token) && expiresAt > DateTime.Now;
        }

        public DateTime ExpirationDate()
        {
            return Until ?? Expired ?? DateTime.UnixEpoch;
        }

        public string GetToken()
        {
            return IsValid() ? Token : string.Empty;
        }

        public static TokenData Empty => new();
    }
}
