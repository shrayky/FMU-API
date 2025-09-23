using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Node.Models;
using Microsoft.Extensions.Logging;

namespace CentralServerExchange.Services
{
    public class CentralServerExchangeService
    {
        private HttpClient HttpClient { get; init; }
        private ILogger Logger { get; init; }
        private string Url { get; init; }

        private CentralServerExchangeService(HttpClient httpClient, ILogger logger, string url)
        {
            HttpClient = httpClient;
            Logger = logger;
            Url = url;
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
                Logger.LogInformation("Sending request to central server: {Url}", Url);

                var response = await HttpClient.PostAsJsonAsync(Url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Logger.LogError("Server returned error: {StatusCode}, {Error}",
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
                Logger.LogError(ex, "Exchange failed");
                return Result.Failure<NodeDataResponse>($"Exchange error: {ex.Message}");
            }
        }

        
    }
}