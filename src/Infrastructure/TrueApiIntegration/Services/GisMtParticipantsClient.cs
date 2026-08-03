using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtParticipantsClient : IGisMtParticipantsClient
{
    private const string ParticipantsPath = "/api/v3/true-api/participants";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<GisMtParticipantsClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtParticipantsClient(
        ILogger<GisMtParticipantsClient> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Получает сведения об участнике оборота, включая товарные группы.
    /// </summary>
    public async Task<Result<List<ParticipantInfo>>> ParticipantsInfo(
        string token,
        string inn,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{GisMtTrueApiHttp.BaseUrl}{ParticipantsPath}?inns={Uri.EscapeDataString(inn)}";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Get, url, token);
            using var response = await client.SendAsync(request, cancellationToken);

            return await GisMtTrueApiHttp.ReadJsonOrFailure<List<ParticipantInfo>>(
                response, "participants", JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<List<ParticipantInfo>>(
                _logger, ex, "Ошибка запроса participants для {Inn}", inn);
        }
    }
}
