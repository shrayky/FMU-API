namespace TrueApiIntegration.Models;

public record RequestParameters
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? DocumentFormat { get; set; }
    public int Limit = 50; //max 1000
    public string? ReceiverInn { get; set; }
}
