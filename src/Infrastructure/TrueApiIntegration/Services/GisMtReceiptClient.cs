using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

/// <summary>
/// Клиент чека ККТ в ГИС МТ: receipt/{id}/info.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtReceiptClient : IGisMtReceiptClient
{
    private const string ReceiptInfoPath = "/api/v4/true-api/receipt/{0}/info";

    private readonly ILogger<GisMtReceiptClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtReceiptClient(ILogger<GisMtReceiptClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<GisMtReceiptInfo>> ReceiptInfo(
        string token,
        string receiptId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var path = string.Format(ReceiptInfoPath, Uri.EscapeDataString(receiptId));
            var url = $"{GisMtTrueApiHttp.BaseUrl}{path}?body=true";

            using var client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            using var request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Get, url, token);
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return Result.Failure<GisMtReceiptInfo>(
                    $"receipt/info HTTP {(int)response.StatusCode}: {errorBody}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var element = document.RootElement;

            if (element.ValueKind == JsonValueKind.Array)
            {
                if (element.GetArrayLength() == 0)
                    return Result.Failure<GisMtReceiptInfo>("Пустой ответ receipt/info");

                element = element[0];
            }

            if (element.ValueKind != JsonValueKind.Object)
                return Result.Failure<GisMtReceiptInfo>("Пустой ответ receipt/info");

            return Result.Success(GisMtReceiptInfo.FromElement(element));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GisMtTrueApiHttp.FailLogged<GisMtReceiptInfo>(
                _logger, ex, "Ошибка запроса receipt/info для {ReceiptId}", receiptId);
        }
    }
}
