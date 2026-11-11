using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

/// <summary>
/// Клиент mods/list: места осуществления деятельности организации в ГИС МТ.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtModsClient : IGisMtModsClient
{
    private const string ModsListPath = "/api/v3/true-api/mods/list";
    private const int MaxRows = 1000;
    private const int MaxPages = 50;

    private readonly ILogger<GisMtModsClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtModsClient(ILogger<GisMtModsClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<List<GisMtModInfo>>> Places(string token, string inn, CancellationToken cancellationToken = default)
    {
        var places = new List<GisMtModInfo>();

        if (string.IsNullOrWhiteSpace(inn))
            return Result.Failure<List<GisMtModInfo>>("Укажите ИНН организации");

        try
        {
            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);

            for (var page = 1; page <= MaxPages; page++)
            {
                var url = $"{GisMtTrueApiHttp.BaseUrl}{ModsListPath}?inns={Uri.EscapeDataString(inn.Trim())}&page={page}";

                using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Get, url, token);
                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    return Result.Failure<List<GisMtModInfo>>($"mods/list HTTP {(int)response.StatusCode}: {errorBody}");
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var pageData = GisMtModsResponse.Parse(body);

                places.AddRange(pageData.Mods);

                if (!pageData.NextPage || places.Count >= MaxRows)
                    break;
            }

            return Result.Success(places.Take(MaxRows).ToList());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<List<GisMtModInfo>>(_logger, ex, "Ошибка запроса mods/list");
        }
    }
}
