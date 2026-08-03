using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtDocumentsClient : IGisMtDocumentsClient
{
    private const string DocListPath = "/api/v4/true-api/doc/list";
    private const string DocInfoPath = "/api/v4/true-api/doc/{0}/info";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<GisMtDocumentsClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtDocumentsClient(ILogger<GisMtDocumentsClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Получает список документов ГИС МТ за период.
    /// </summary>
    public async Task<Result<GisMtDocListResponse>> DocumentList(
        string token,
        string productGroup,
        string receiverInn,
        DateTime dateFrom,
        DateTime dateTo,
        IEnumerable<string> documentTypes,
        string? did,
        string? orderedColumnValue,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParts = new List<string>
            {
                $"pg={Uri.EscapeDataString(productGroup)}",
                $"dateFrom={Uri.EscapeDataString(dateFrom.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'"))}",
                $"dateTo={Uri.EscapeDataString(dateTo.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'"))}",
                "limit=100",
                "order=ASC"
            };

            foreach (var documentType in documentTypes)
                queryParts.Add($"documentType={Uri.EscapeDataString(documentType)}");

            if (!string.IsNullOrWhiteSpace(did) && !string.IsNullOrWhiteSpace(orderedColumnValue))
            {
                queryParts.Add($"did={Uri.EscapeDataString(did)}");
                queryParts.Add($"orderedColumnValue={Uri.EscapeDataString(orderedColumnValue)}");
                queryParts.Add("pageDir=NEXT");
            }

            var url = $"{GisMtTrueApiHttp.BaseUrl}{DocListPath}?{string.Join("&", queryParts)}";

            _logger.LogInformation(
                "Запрос списка документов ГИС МТ: inn={Inn}, pg={Pg}, from={From}, to={To}",
                receiverInn, productGroup, dateFrom, dateTo);

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Get, url, token);
            using var response = await client.SendAsync(request, cancellationToken);

            return await GisMtTrueApiHttp.ReadJsonOrFailure<GisMtDocListResponse>(
                response, "doc/list", JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<GisMtDocListResponse>(
                _logger, ex, "Ошибка запроса doc/list");
        }
    }

    /// <summary>
    /// Получает содержимое документа по идентификатору.
    /// </summary>
    public async Task<Result<GisMtDocInfoResponse>> DocumentInfo(
        string token,
        string documentId,
        string? productGroup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParts = new List<string> { "body=true", "content=true" };
            if (!string.IsNullOrWhiteSpace(productGroup))
                queryParts.Add($"pg={Uri.EscapeDataString(productGroup)}");

            var path = string.Format(DocInfoPath, Uri.EscapeDataString(documentId));
            var url = $"{GisMtTrueApiHttp.BaseUrl}{path}?{string.Join("&", queryParts)}";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Get, url, token);
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure<GisMtDocInfoResponse>(
                    $"doc/info HTTP {(int)response.StatusCode}: {errorBody}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var info = document.RootElement.ValueKind switch
            {
                JsonValueKind.Array when document.RootElement.GetArrayLength() > 0
                    => document.RootElement[0].Deserialize<GisMtDocInfoResponse>(JsonOptions),
                JsonValueKind.Object
                    => document.RootElement.Deserialize<GisMtDocInfoResponse>(JsonOptions),
                _ => null
            };

            return info is null
                ? Result.Failure<GisMtDocInfoResponse>("Пустой ответ doc/info")
                : Result.Success(info);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<GisMtDocInfoResponse>(
                _logger, ex, "Ошибка запроса doc/info для {DocumentId}", documentId);
        }
    }
}
