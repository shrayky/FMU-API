using FmuApiDomain.ViewData.ApplicationMonitoring.Dto;
using FmuApiDomain.ViewData.Dto;

namespace FmuApiDomain.ViewData.ApplicationMonitoring.Interfaces;

public interface IMonitoringInformation
{
    Task<MonitoringData> Collect();
}