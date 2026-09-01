using System.Diagnostics;
using FmuApiDomain.Attributes;
using FmuApiDomain.Connectivity.Interfaces;
using FmuApiDomain.Connectivity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.Connectivity.Services;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class CrptEspConnectivityService(
    IHttpClientFactory httpClientFactory,
    ICrptEspHostStore hostStore) : ICrptEspConnectivityService
{
    public const string HttpClientName = "crptEspCheck";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ICrptEspHostStore _hostStore = hostStore;

    public CrptEspCheckResult ListHosts()
    {
        var items = _hostStore.Load()
            .Select((host, index) => new CrptEspCheckItem
            {
                Index = index + 1,
                Address = host.Url,
                Group = host.Group
            })
            .ToList();

        return new CrptEspCheckResult
        {
            Items = items,
            Total = items.Count
        };
    }

    public async Task<CrptEspCheckResult> CheckAll(CancellationToken cancellationToken)
    {
        var hosts = _hostStore.Load();
        using var client = _httpClientFactory.CreateClient(HttpClientName);

        var tasks = hosts
            .Select((host, index) => CheckOne(client, index + 1, host, cancellationToken));

        var items = await Task.WhenAll(tasks);
        var ordered = items.OrderBy(item => item.Index).ToList();

        return new CrptEspCheckResult
        {
            Items = ordered,
            Available = ordered.Count(item => item.Available == true),
            Unavailable = ordered.Count(item => item.Available == false),
            Total = ordered.Count
        };
    }

    private static async Task<CrptEspCheckItem> CheckOne(
        HttpClient client,
        int index,
        CrptEspHost host,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{host.Url}/");
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            stopwatch.Stop();

            return new CrptEspCheckItem
            {
                Index = index,
                Address = host.Url,
                Group = host.Group,
                Available = true,
                ElapsedMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            stopwatch.Stop();

            return new CrptEspCheckItem
            {
                Index = index,
                Address = host.Url,
                Group = host.Group,
                Available = false,
                ElapsedMs = (int)stopwatch.ElapsedMilliseconds
            };
        }
    }
}
