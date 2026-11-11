using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

/// <summary>
/// Клиент положения кода идентификации в ГИС МТ: cises/history.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtCisHistoryClient : IGisMtCisHistoryClient
{
    private const string HistoryPath = "/api/v4/true-api/cises/history";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<GisMtCisHistoryClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtCisHistoryClient(ILogger<GisMtCisHistoryClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<List<GisMtCisHistoryRecord>>> History(
        string token,
        string cis,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{GisMtTrueApiHttp.BaseUrl}{HistoryPath}?cis={Uri.EscapeDataString(cis)}";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Post, url, token);
            using var response = await client.SendAsync(request, cancellationToken);

            return await GisMtTrueApiHttp.ReadJsonOrFailure<List<GisMtCisHistoryRecord>>(
                response, "cises/history", JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<List<GisMtCisHistoryRecord>>(
                _logger, ex, "Ошибка запроса cises/history для {Cis}", cis);
        }
    }
}
