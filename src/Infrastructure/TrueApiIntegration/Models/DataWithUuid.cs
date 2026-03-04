namespace TrueApiIntegration.Models;

public record DataWithUuid
{
    public string Uuid { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
