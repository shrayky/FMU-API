using FmuApiApplication.Monitoring.Dto;

namespace FmuApiApplication.Monitoring.Interfaces;

public interface IMonitoringInformation
{
    Task<MonitoringData> Collect();
}