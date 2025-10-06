namespace FmuApiDomain.ViewData.ApplicationMonitoring.Dto;

public record LocalModuleState
{
    public string Address { get; init; } = string.Empty;
    public string Version { get; init; }  = string.Empty;
    public DateTime LastSyncTime { get; init; }
    public string State { get; init; }   = string.Empty;
    public bool IsReady { get; init; }
}