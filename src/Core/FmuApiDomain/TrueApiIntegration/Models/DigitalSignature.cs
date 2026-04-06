namespace FmuApiDomain.TrueApiIntegration.Models;

public record DigitalSignature
{
    public string Presentation {  get; set; } = string.Empty;
    public DateTime WorkUntil { get; set; }
    public string Inn { get; set; } = string.Empty;
    public string Number {  get; set; } = string.Empty;
}
