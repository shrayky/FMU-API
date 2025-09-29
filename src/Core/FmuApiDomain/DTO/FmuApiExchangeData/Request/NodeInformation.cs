using System.Text.Json.Serialization;

namespace FmuApiDomain.DTO.FmuApiExchangeData.Request;

public record NodeInformation
{
    [JsonPropertyName("architecture")]
    public string Architecture {get; init;} = Environment.Is64BitOperatingSystem ? "x64" : "x86";
    
    [JsonPropertyName("os")]
    public string Os {get; init;} = OperatingSystem.IsWindows() ? "windows" : "linux";
}