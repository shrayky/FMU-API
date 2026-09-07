using FmuApiDomain.Connectivity.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.InternetConnectionCheck.Workers;

public class InternetConnectionCheckWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<InternetConnectionCheckWorker> logger) : BackgroundService
{
    private readonly int _checkPeriodMinutes = 2;
    private DateTime _nextWorkDate = DateTime.Now;
    private readonly int _checkInterval = 60_000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_nextWorkDate >= DateTime.Now)
            {
                await Task.Delay(_checkInterval, stoppingToken);
                continue;
            }

            _nextWorkDate = DateTime.Now.AddMinutes(_checkPeriodMinutes);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var checkService = scope.ServiceProvider.GetRequiredService<IInternetConnectionCheckService>();
                await checkService.PerformCheck(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка проверки доступности интернета");
            }
        }
    }
}
