namespace FmuApiDomain.ViewData.ApplicationMonitoring.Dto;

public record MarkChecksInformation
{
    public int Total { get; init; } = 0;
    public int SuccessfulOnline  { get; init; } = 0;
    public int SuccessfulOffline  { get; init; } = 0;
    public double SuccessRate  { get; init; } = 0;
}