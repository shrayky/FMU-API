using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Database.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Http;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CouchDb.Services
{
    [AutoRegisterService(ServiceLifetime.Singleton)]
    public class StatusCouchDbService : IStatusDbService
    {
        private readonly ILogger<StatusCouchDbService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _scopeFactory;

        public StatusCouchDbService(ILogger<StatusCouchDbService> logger, IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _scopeFactory = scopeFactory;
        }

        public async Task<bool> CheckAvailability(string databaseUrl, CancellationToken cancellationToken = default)
        {
            var httpClientResult = _httpClientFactory.CreateClientSafely("CouchDbState", _logger);
            if (httpClientResult.IsFailure)
            {
                _logger.LogError("Не удалось создать HttpClient: {Error}", httpClientResult.Error);
                return false;
            }

            using var httpClient = httpClientResult.Value;
            httpClient.BaseAddress = new Uri(databaseUrl);

            var responseResult = await httpClient.SendRequestSafelyAsync(
                client => client.GetAsync("", cancellationToken),
                _logger,
                "проверка состояния CouchDB");

            if (responseResult.IsFailure)
            {
                _logger.LogError("Ошибка при проверке состояния CouchDB: {Error}", responseResult.Error);
                return false;
            }

            return responseResult.Value.IsSuccessStatusCode;
        }

        public async Task<bool> EnsureDatabasesExists(CouchDbConnection connection, string[] databasesNames, CancellationToken cancellationToken)
        {
            var httpClientResult = _httpClientFactory.CreateClientSafely("CouchDbState", _logger);

            if (httpClientResult.IsFailure)
            {
                _logger.LogError("Не удалось создать HttpClient: {Error}", httpClientResult.Error);
                return false;
            }

            using var httpClient = httpClientResult.Value;
            httpClient.BaseAddress = new Uri(connection.NetAddress);

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{connection.UserName}:{connection.Password}"));
            httpClient.DefaultRequestHeaders.Authorization = new("Basic", authToken);

            foreach (var dbName in databasesNames)
            {
                var responseResult = await httpClient.SendRequestSafelyAsync(client => client.GetAsync($"/{dbName}", cancellationToken),
                                                                                _logger,
                                                                                $"проверка существования {dbName}");

                if (responseResult.IsFailure)
                    return false;

                if (responseResult.Value.IsSuccessStatusCode)
                    continue;

                if (responseResult.Value.StatusCode == HttpStatusCode.NotFound)
                {
                    var creationResult = await httpClient.SendRequestSafelyAsync(client => client.PutAsync($"/{dbName}", null, cancellationToken),
                                                                                    _logger,
                                                                                    $"создание (put) {dbName}");

                    if (creationResult.IsSuccess)
                    {
                        _logger.LogInformation("База данных {DatabaseName} успешно создана", dbName);
                    }
                    else
                    {
                        _logger.LogError("Не удалось создать базу данных {DatabaseName}: {StatusCode}", dbName, creationResult.Value.StatusCode);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> EnsureIndexesExist(CouchDbConnection connection, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Проверка наличия индексов для баз данных CouchDB.");

            var httpClientResult = _httpClientFactory.CreateClientSafely("CouchDbState", _logger);
            if (httpClientResult.IsFailure)
            {
                _logger.LogError("Не удалось создать HttpClient: {Error}", httpClientResult.Error);
                return false;
            }

            using var httpClient = httpClientResult.Value;
            httpClient.BaseAddress = new Uri(connection.NetAddress);

            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{connection.UserName}:{connection.Password}"));
            httpClient.DefaultRequestHeaders.Authorization = new("Basic", authToken);

            var marksIndexesCreated = await CreateMarksIndexes(httpClient, cancellationToken);
            var statisticIndexesCreated = await CreateStatisticIndexes(httpClient, cancellationToken);

            if (marksIndexesCreated && statisticIndexesCreated)
            {
                _logger.LogInformation("Индексы для баз данных CouchDB созданы успешно");
                return true;
            }

            return false;
        }

        private async Task<bool> CreateMarksIndexes(HttpClient httpClient, CancellationToken cancellationToken)
        {
            var indexes = new[]
            {
                    new { name = "mark-id-idx", index = new { fields = new[] { "data.markId" } } },
                    new { name = "mark-data-idx", index = new { fields = new[] { "data" } } },
                    new { name = "timeStamp-data-idx", index = new { fields = new[] { "data.trueApiAnswerProperties.reqTimestamp" } } }
                };

            return await CreateIndexesForDatabase(httpClient, DatabaseNames.MarksDbName, indexes, cancellationToken);
        }

        private async Task<bool> CreateStatisticIndexes(HttpClient httpClient, CancellationToken cancellationToken)
        {
            var indexes = new[]
            {
                    new { name = "date-time-idx", index = new { fields = new[] { "data.checkDate" } } },
                    new { name = "date-sgtin", index = new { fields = new[] { "data.sGtin" } } }
                };

            return await CreateIndexesForDatabase(httpClient, DatabaseNames.MarkCheckingStatistic, indexes, cancellationToken);
        }

        private async Task<bool> CreateIndexesForDatabase(HttpClient httpClient, string databaseName, object[] indexes, CancellationToken cancellationToken)
        {
            foreach (var index in indexes)
            {
                var indexJson = JsonSerializer.Serialize(index);
                var content = new StringContent(indexJson, Encoding.UTF8, "application/json");

                var responseResult = await httpClient.SendRequestSafelyAsync(
                    client => client.PostAsync($"/{databaseName}/_index", content, cancellationToken),
                    _logger,
                    $"создание индекса для базы {databaseName}");

                if (responseResult.IsSuccess)
                    _logger.LogDebug("Индекс для базы {DatabaseName} создан успешно", databaseName);
                else
                    _logger.LogWarning("Не удалось создать индекс для базы {DatabaseName}: {StatusCode}", databaseName, responseResult.Value.StatusCode);
            }

            return true;
        }
    }
}

