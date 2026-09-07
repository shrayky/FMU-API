using FmuApiDomain.Attributes;
using FmuApiDomain.Connectivity.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Http;
using System.Net;

namespace FmuApiApplication.InternetConnectionCheck.Services;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class InternetConnectionCheckService(
    IParametersService parametersService,
    IHttpClientFactory httpClientFactory,
    IApplicationState applicationState,
    ILogger<InternetConnectionCheckService> logger) : IInternetConnectionCheckService
{
    public async Task PerformCheck(CancellationToken cancellationToken)
    {
        var configuration = await parametersService.CurrentAsync();

        var hosts = configuration.HostsToPing
            .Select(address => address.Value.Trim())
            .Where(address => address != string.Empty)
            .ToList();

        if (hosts.Count == 0)
            return;

        var online = false;

        foreach (var address in hosts)
        {
            online = await CheckHost(
                address,
                configuration.HttpRequestTimeouts.CheckInternetConnectionTimeout,
                cancellationToken);

            if (online)
                break;
        }

        applicationState.SetOnlineStatus(online);
    }

    private async Task<bool> CheckHost(
        string siteAddress,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(InternetConnectionCheckRegistration.HttpClientName);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var answer = await client.GetAsync(BuildCheckUrl(siteAddress), cancellationToken);

            return IsAvailableStatus(answer.StatusCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Ошибка проверки доступности интернета: {err}", ex.GetBaseException().Message);
            return false;
        }
    }

    private static string BuildCheckUrl(string siteAddress)
    {
        siteAddress = siteAddress.Replace("https://", "");
        siteAddress = siteAddress.Replace("http://", "");

        if (ForceHttp11MessageHandler.IsRequiredOnThisOs)
        {
            return $"http://{siteAddress}";
        }
        else
        {
            return $"https://{siteAddress}";
        }
    }

    private static bool IsAvailableStatus(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code is >= 200 and < 400;
    }
}
