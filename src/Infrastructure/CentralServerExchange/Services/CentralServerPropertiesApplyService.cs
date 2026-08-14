using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.CentralServiceExchange.Models.Answer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class CentralServerPropertiesApplyService
{
    private readonly ILogger<CentralServerPropertiesApplyService> _logger;
    private readonly IParametersService _parametersService;

    public CentralServerPropertiesApplyService(
        ILogger<CentralServerPropertiesApplyService> logger,
        IParametersService parametersService)
    {
        _logger = logger;
        _parametersService = parametersService;
    }

    public async Task ApplyIfChanged(CentralServerProperties? properties)
    {
        try
        {
            if (properties == null)
                return;

            var settings = await _parametersService.CurrentAsync().ConfigureAwait(false);
            var central = settings.FmuApiCentralServer;
            var changed = false;

            if (!string.IsNullOrWhiteSpace(properties.ExchangeServerAddresses)
                && !string.Equals(
                    central.Address.Trim(),
                    properties.ExchangeServerAddresses.Trim(),
                    StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Обновляю адреса серверов обмена с Central: {Old} -> {New}",
                    central.Address,
                    properties.ExchangeServerAddresses);

                central.Address = properties.ExchangeServerAddresses.Trim();
                changed = true;
            }

            if (properties.ExchangeRequestInterval > 0
                && central.ExchangeRequestInterval != properties.ExchangeRequestInterval)
            {
                _logger.LogInformation(
                    "Обновляю интервал обмена с Central: {Old} -> {New} мин.",
                    central.ExchangeRequestInterval,
                    properties.ExchangeRequestInterval);

                central.ExchangeRequestInterval = properties.ExchangeRequestInterval;
                changed = true;
            }

            if (properties.SchedulerUpdateDownload.Count > 0
                && !SchedulesEqual(central.SchedulerUpdateInstall, properties.SchedulerUpdateDownload))
            {
                _logger.LogInformation(
                    "Обновляю расписание загрузки обновлений с Central ({Count} интервалов)",
                    properties.SchedulerUpdateDownload.Count);

                central.SchedulerUpdateInstall = properties.SchedulerUpdateDownload
                    .Select(x => new ScheduleTime
                    {
                        Id = x.Id,
                        BeginTime = x.BeginTime,
                        EndTime = x.EndTime
                    })
                    .ToList();
                changed = true;
            }

            if (!changed)
                return;

            await _parametersService.UpdateAsync(settings).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось применить centralServerProperties из ответа Central");
        }
    }

    private static bool SchedulesEqual(
        List<ScheduleTime> local,
        List<ScheduleTimeDto> remote)
    {
        if (local.Count != remote.Count)
            return false;

        var localNorm = local
            .OrderBy(x => x.Id)
            .ThenBy(x => x.BeginTime)
            .Select(x => (x.Id, x.BeginTime, x.EndTime));

        var remoteNorm = remote
            .OrderBy(x => x.Id)
            .ThenBy(x => x.BeginTime)
            .Select(x => (x.Id, x.BeginTime, x.EndTime));

        return localNorm.SequenceEqual(remoteNorm);
    }
}
