using FmuApiDomain.ViewData.Dto;

namespace FmuApiApplication.ViewData.ApplicationMonitoring.Dto;

public record MonitoringData
{
    public string CouchDbOnLine { get; init; } = string.Empty;
    public List<LocalModuleState> StateOfLocalModules { get; init; } = [];
    public MarkCheksStatistics MarkCheksStatistics { get; init; } = new();
    public List<TsPiotState> TsPiotStates { get; init; } = new();
}