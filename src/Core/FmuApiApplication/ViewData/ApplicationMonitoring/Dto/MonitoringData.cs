using FmuApiApplication.StateCollectors.Models;
using FmuApiDomain.ViewData.Dto;

namespace FmuApiApplication.ViewData.ApplicationMonitoring.Dto;

public record MonitoringData
{
    public string CouchDbOnLine { get; init; } = string.Empty;
    public List<LocalModuleStateInformation> StateOfLocalModules { get; init; } = [];
    public MarkCheksStatistics MarkCheksStatistics { get; init; } = new();
    public List<TsPiotStateInfotmation> TsPiotStates { get; init; } = new();
}