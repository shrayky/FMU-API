using FmuApiApplication.ViewData.ApplicationMonitoring.Dto;

namespace FmuApiApplication.ViewData.ApplicationMonitoring.Interfaces;

public interface IMonitoringInformation
{
    Task<MonitoringData> Collect();
}