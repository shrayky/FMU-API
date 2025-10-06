using FmuApiDomain.ViewData.Dto;

namespace FmuApiDomain.ViewData.ApplicationMonitoring.Dto;

public record MonitoringData
{
    public string CouchDbOnLine { get; init; } = string.Empty;
    public List<LocalModuleState> StateOfLocalModules { get; init; } = [];
    public MarkCheksStatistics MarkCheksStatistics { get; init; } = new();
}