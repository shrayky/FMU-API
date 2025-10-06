using FmuApiDomain.ViewData.ApplicationMonitoring.Dto;

namespace FmuApiDomain.ViewData.Dto;

public record MarkCheksStatistics
{
    public MarkChecksInformation Today { get; init; } = new();
    public MarkChecksInformation Last7Days { get; init; } = new();
    public MarkChecksInformation Last30Days { get; init; } = new();
}