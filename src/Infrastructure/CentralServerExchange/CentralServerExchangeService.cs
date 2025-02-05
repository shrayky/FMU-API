using CSharpFunctionalExtensions;
using FmuApiDomain.Node.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace CentralServerExchange
{
    public class CentralServerExchangeService
    {
        private HttpClient _httpClient { get; init; }
        private ILogger _logger { get; init; }
        private string _url { get; init; }

        private CentralServerExchangeService(HttpClient httpClient, ILogger logger, string url)
        {
            _httpClient = httpClient;
            _logger = logger;
            _url = url;
        }

        public static CentralServerExchangeService Create(HttpClient httpClient, ILogger logger, string url)
        {
            return new(httpClient, logger, url);
        }

        public async Task<Result<NodeDataResponse>> ActExchange(NodeDataRequest request)
        {
            return await SafeActExchange(request);
        }

        private async Task<Result<NodeDataResponse>> SafeActExchange(NodeDataRequest request)
        {
            try
            {
                _logger.LogInformation("Sending request to central server: {Url}", _url);

                var response = await _httpClient.PostAsJsonAsync(_url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Server returned error: {StatusCode}, {Error}",
                        response.StatusCode, error);

                    return Result.Failure<NodeDataResponse>(
                        $"Server returned {response.StatusCode}: {error}");
                }

                var result = await response.Content.ReadFromJsonAsync<NodeDataResponse>();
                if (result is null)
                {
                    return Result.Failure<NodeDataResponse>("Empty response from server");
                }

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exchange failed");
                return Result.Failure<NodeDataResponse>($"Exchange error: {ex.Message}");
            }
        }

        
    }
}