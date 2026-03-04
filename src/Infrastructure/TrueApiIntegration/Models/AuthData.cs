using System.Text.Json.Serialization;

namespace TrueApiIntegration.Models;

public record AuthData
{
    public string Token { get; set; } = string.Empty;
    
    public string Code { get; set; } = string.Empty;
    
    [JsonPropertyName("error_message")]
    public string ErrorMessage {  get; set; } = string.Empty;

    public string Description {  get; set; } = string.Empty;
}
