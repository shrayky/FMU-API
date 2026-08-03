using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using FmuApiDomain.TrueApi;
using FmuApiDomain.TrueApi.ProductInfo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtCisesClient : IGisMtCisesClient
{
    private const string CisesInfoPath = "/api/v3/true-api/cises/info";
    private const string CisesSearchPath = "/api/v4/true-api/cises/search";
    private const string ProductInfoPath = "/api/v4/true-api/product/info";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<GisMtCisesClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtCisesClient(ILogger<GisMtCisesClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Получает общедоступную информацию о КИ.
    /// </summary>
    public async Task<Result<List<CisInfoResponseItem>>> CisesInfo(
        string token,
        IReadOnlyList<string> cises,
        string? productGroup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (cises.Count == 0)
                return Result.Success(new List<CisInfoResponseItem>());

            var url = string.IsNullOrWhiteSpace(productGroup)
                ? $"{GisMtTrueApiHttp.BaseUrl}{CisesInfoPath}"
                : $"{GisMtTrueApiHttp.BaseUrl}{CisesInfoPath}?pg={Uri.EscapeDataString(productGroup)}";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Post, url, token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(cises),
                Encoding.UTF8,
                "application/json");
            using var response = await client.SendAsync(request, cancellationToken);

            return await GisMtTrueApiHttp.ReadJsonOrFailure<List<CisInfoResponseItem>>(
                response, "cises/info", JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<List<CisInfoResponseItem>>(
                _logger, ex, "Ошибка запроса cises/info");
        }
    }

    /// <summary>
    /// Ищет КИ по фильтрам.
    /// </summary>
    public async Task<Result<CisSearchResponse>> SearchCises(
        string token,
        CisSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{GisMtTrueApiHttp.BaseUrl}{CisesSearchPath}";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var httpRequest = GisMtTrueApiHttp.CreateRequest(HttpMethod.Post, url, token);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(request, JsonOptions),
                Encoding.UTF8,
                "application/json");
            using var response = await client.SendAsync(httpRequest, cancellationToken);

            return await GisMtTrueApiHttp.ReadJsonOrFailure<CisSearchResponse>(
                response, "cises/search", JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<CisSearchResponse>(
                _logger, ex, "Ошибка запроса cises/search");
        }
    }

    /// <summary>
    /// Получает информацию о товарах по списку GTIN.
    /// </summary>
    public async Task<Result<ProductsInformationTrueApi>> ProductInfo(
        string token,
        List<string> gtins,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{GisMtTrueApiHttp.BaseUrl}{ProductInfoPath}";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Post, url, token);
            request.Content = JsonContent.Create(new GtinsArray(gtins));
            using var response = await client.SendAsync(request, cancellationToken);

            return await GisMtTrueApiHttp.ReadJsonOrFailure<ProductsInformationTrueApi>(
                response, "product/info", JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<ProductsInformationTrueApi>(
                _logger, ex, "Ошибка запроса product/info");
        }
    }
}
