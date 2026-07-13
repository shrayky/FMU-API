using CouchDb.DatabaseScheme;
using CouchDb.Interfaces;
using CouchDb.Models;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Http;
using System.Text;
using System.Text.Json;

namespace CouchDb.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class IndexingService : IIndexingService
{
    private readonly ILogger<IndexingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexingService(ILogger<IndexingService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result> EnsureIndexesExist(CouchDbConnection connection, CancellationToken cancellationToken)
    {
        var httpClientResult = _httpClientFactory.CreateClientSafely("CouchDbState", _logger);

        if (httpClientResult.IsFailure)
        {
            var err = $"Не удалось создать HttpClient: {httpClientResult.Error}";
            return Result.Failure(err);
        }

        using var httpClient = httpClientResult.Value;
        httpClient.BaseAddress = new Uri(connection.NetAddress);

        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{connection.UserName}:{connection.Password}"));
        httpClient.DefaultRequestHeaders.Authorization = new("Basic", authToken);

        var success = 0;

        var indexShema = DatabaseIndexes.DatabaseIndexSchema();

        foreach (var index in indexShema)
        {
            if (await CreateIndexesForDatabase(httpClient, index.Key, index.Value, cancellationToken))
                success++;
        }

        if (success != indexShema.Count)
        {
            var err = "Не удалось создать индексы для баз данных CouchDB!";
            return Result.Failure(err);
        }

        return Result.Success();
    }

    private async Task<bool> CreateIndexesForDatabase(
        HttpClient httpClient,
        string databaseName,
        CouchDbIndexDefinition[] databaseIndexes,
        CancellationToken cancellationToken)
    {
        var existingIndexResult = await ExistingIndexNames(httpClient, databaseName, cancellationToken);

        if (existingIndexResult.IsFailure)
        {
            _logger.LogError(existingIndexResult.Error);
            return false;
        }

        var existingNames = existingIndexResult.Value;
        var allSucceeded = true;

        foreach (var index in databaseIndexes)
        {
            if (existingNames.Contains(index.Name))
            {
                _logger.LogDebug("Индекс {IndexName} для базы {DatabaseName} уже существует", index.Name, databaseName);
                continue;
            }

            var indexJson = JsonSerializer.Serialize(index);
            var content = new StringContent(indexJson, Encoding.UTF8, "application/json");

            var responseResult = await httpClient.SendRequestSafelyAsync(
                client => client.PostAsync($"/{databaseName}/_index", content, cancellationToken),
                _logger,
                $"создание индекса {index.Name} для базы {databaseName}");

            if (responseResult.IsFailure)
            {
                _logger.LogWarning("Не удалось создать индекс {IndexName} для базы {DatabaseName}: {Error}", index.Name, databaseName, responseResult.Error);
                allSucceeded = false;
                continue;
            }

            if (!responseResult.Value.IsSuccessStatusCode)
            {
                _logger.LogWarning("Не удалось создать индекс {IndexName} для базы {DatabaseName}: {StatusCode}", index.Name, databaseName, responseResult.Value.StatusCode);
                allSucceeded = false;
                continue;
            }

            _logger.LogDebug("Индекс {IndexName} для базы {DatabaseName} создан успешно", index.Name, databaseName);
        }

        return allSucceeded;
    }

    private async Task<Result<HashSet<string>>> ExistingIndexNames(
        HttpClient httpClient,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var responseResult = await httpClient.SendRequestSafelyAsync(
            client => client.GetAsync($"/{databaseName}/_index", cancellationToken),
            _logger,
            $"получение списка индексов для базы {databaseName}");

        if (responseResult.IsFailure)
        {
            return Result.Failure<HashSet<string>>($"Не удалось получить список индексов для базы {databaseName}: {responseResult.Error}.");
        }

        using var response = responseResult.Value;
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<HashSet<string>>($"Не удалось получить список индексов для базы {databaseName}: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var indexList = JsonSerializer.Deserialize<CouchDbIndexListResponse>(json);

        return indexList?.Indexes
            .Select(i => i.Name)
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
    }
}