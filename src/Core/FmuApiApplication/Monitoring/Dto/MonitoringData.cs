using FmuApiApplication.LocalModule.Models;
using FmuApiApplication.TsPiot.Models;

namespace FmuApiApplication.Monitoring.Dto;

public record MonitoringData
{
    public string CouchDbOnLine { get; init; } = string.Empty;
    public List<LocalModuleStateInformation> StateOfLocalModules { get; init; } = [];
    public MarkChecksStatistics MarkCheksStatistics { get; init; } = new();
    public List<TsPiotStateInformation> TsPiotStates { get; init; } = new();
}