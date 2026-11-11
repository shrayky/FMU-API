using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.GisMt.Interfaces;
using FmuApiDomain.GisMt.Journal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrueApiIntegration.Services;

/// <summary>
/// Клиент создания документа в ГИС МТ: lk/documents/create.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class GisMtLkDocumentsClient : IGisMtLkDocumentsClient
{
    private const string CreateDocumentPath = "/api/v3/true-api/lk/documents/create";

    private readonly ILogger<GisMtLkDocumentsClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GisMtLkDocumentsClient(ILogger<GisMtLkDocumentsClient> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<string>> CreateDocument(
        string token,
        string productGroup,
        string type,
        string productDocument,
        string signature,
        CancellationToken cancellationToken = default)
    {
        HttpClient client;
        HttpRequestMessage request;

        try
        {
            var url = $"{GisMtTrueApiHttp.BaseUrl}{CreateDocumentPath}?pg={Uri.EscapeDataString(productGroup)}";

            client = _httpClientFactory.CreateClient(GisMtTrueApiHttp.HttpClientName);
            request = GisMtTrueApiHttp.CreateRequest(HttpMethod.Post, url, token);
            request.Content = JsonContent.Create(new CreateDocumentRequest
            {
                Type = type,
                ProductDocument = Convert.ToBase64String(Encoding.UTF8.GetBytes(productDocument)),
                Signature = signature
            });
        }
        catch (Exception ex)
        {
            // Запрос ещё не уходил: это обычная ошибка подготовки, повторять отправку безопасно.
            return GisMtTrueApiHttp.FailLogged<string>(_logger, ex, "Ошибка подготовки документа для ГИС МТ");
        }

        using (client)
        using (request)
        {
            try
            {
                using var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    return Result.Failure<string>($"lk/documents/create HTTP {(int)response.StatusCode}: {errorBody}");
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var documentId = GisMtDocumentId.Parse(body);

                if (documentId.Length == 0)
                {
                    // Успешный ответ без идентификатора: документ мог быть создан, повтор создал бы второй.
                    throw new GisMtSendUnconfirmedException(
                        "lk/documents/create вернул успех без идентификатора документа");
                }

                return Result.Success(documentId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (GisMtSendUnconfirmedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Запрос уже ушёл: Честный знак мог принять документ, поэтому это не обычная ошибка отправки.
                _logger.LogError(ex, "Ответ на lk/documents/create не получен");

                throw new GisMtSendUnconfirmedException(ex.Message, ex);
            }
        }
    }

    private sealed class CreateDocumentRequest
    {
        [JsonPropertyName("document_format")]
        public string DocumentFormat { get; init; } = "MANUAL";

        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName("product_document")]
        public string ProductDocument { get; init; } = string.Empty;

        [JsonPropertyName("signature")]
        public string Signature { get; init; } = string.Empty;
    }
}
