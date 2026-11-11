using System.Text;
using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

/// <summary>
/// Клиент вложений агрегатов ГИС МТ: cises/aggregated/list.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtCisAggregationClient : IGisMtCisAggregationClient
{
    private const string AggregatedListPath = "/api/v4/true-api/cises/aggregated/list";

    private readonly ILogger<GisMtCisAggregationClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtCisAggregationClient(ILogger<GisMtCisAggregationClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<Dictionary<string, List<string>>>> AggregatedList(
        string token,
        IReadOnlyList<string> cises,
        string? productGroup,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (cises.Count == 0)
                return Result.Success(new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase));

            var url = string.IsNullOrWhiteSpace(productGroup)
                ? $"{GisMtTrueApiHttp.BaseUrl}{AggregatedListPath}"
                : $"{GisMtTrueApiHttp.BaseUrl}{AggregatedListPath}?pg={Uri.EscapeDataString(productGroup)}";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Post, url, token);
            request.Content = new StringContent(
                JsonSerializer.Serialize(cises),
                Encoding.UTF8,
                "application/json");
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure<Dictionary<string, List<string>>>(
                    $"cises/aggregated/list HTTP {(int)response.StatusCode}: {errorBody}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return Result.Success(result);

            foreach (var aggregate in document.RootElement.EnumerateObject())
                result[aggregate.Name] = ReadChildren(aggregate.Value);

            return Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<Dictionary<string, List<string>>>(
                _logger, ex, "Ошибка запроса cises/aggregated/list");
        }
    }

    /// <summary>
    /// Вложения упаковки: объект «код → сведения» либо массив кодов.
    /// </summary>
    private static List<string> ReadChildren(JsonElement value)
    {
        var children = new List<string>();

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var child in value.EnumerateObject())
                    children.Add(child.Name);
                break;

            case JsonValueKind.Array:
                foreach (var child in value.EnumerateArray())
                {
                    var code = child.ValueKind switch
                    {
                        JsonValueKind.String => child.GetString(),
                        JsonValueKind.Object when child.TryGetProperty("cis", out var cis) => cis.GetString(),
                        _ => null
                    };

                    if (!string.IsNullOrWhiteSpace(code))
                        children.Add(code);
                }
                break;
        }

        return children;
    }
}
