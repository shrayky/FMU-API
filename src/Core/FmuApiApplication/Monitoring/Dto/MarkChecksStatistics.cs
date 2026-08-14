namespace FmuApiApplication.Monitoring.Dto;

public record MarkChecksStatistics
{
    public MarkChecksInformation Today { get; init; } = new();
    public MarkChecksInformation Last7Days { get; init; } = new();
    public MarkChecksInformation Last30Days { get; init; } = new();
}