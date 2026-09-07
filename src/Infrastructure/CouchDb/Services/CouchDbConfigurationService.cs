using CouchDb.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CouchDb.Services;

/// <summary>
/// Сверяет _config ноды CouchDB с настройками приложения и исправляет расхождения.
/// </summary>
[AutoRegisterService(ServiceLifetime.Singleton)]
public class CouchDbConfigurationService(
    ILogger<CouchDbConfigurationService> logger,
    IHttpClientFactory httpClientFactory) : ICouchDbConfigurationService
{
    private const string DisabledLogLevel = "none";

    private readonly ILogger<CouchDbConfigurationService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<Result> EnsureSettings(CouchDbConnection connection, CancellationToken cancellationToken)
    {
        if (!connection.DisableDbLog)
            return Result.Success();

        try
        {
            var httpClientResult = CreateAuthorizedClient(connection);

            if (httpClientResult.IsFailure)
                return Result.Failure(httpClientResult.Error);

            using var httpClient = httpClientResult.Value;

            var nodeNameResult = await NodeName(httpClient, cancellationToken);

            if (nodeNameResult.IsFailure)
                return Result.Failure(nodeNameResult.Error);

            var logLevelResult = await EnsureLogLevelDisabled(httpClient, nodeNameResult.Value, cancellationToken);

            if (logLevelResult.IsFailure)
                return logLevelResult;

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка применения настроек CouchDB");
            return Result.Failure(ex.Message);
        }
    }

    private async Task<Result> EnsureLogLevelDisabled(HttpClient httpClient, string nodeName, CancellationToken cancellationToken)
    {
        var logLevelResult = await LogLevel(httpClient, nodeName, cancellationToken);

        if (logLevelResult.IsFailure)
            return Result.Failure(logLevelResult.Error);

        if (string.Equals(logLevelResult.Value, DisabledLogLevel, StringComparison.OrdinalIgnoreCase))
            return Result.Success();

        return await SetLogLevel(httpClient, nodeName, DisabledLogLevel, cancellationToken);
    }

    private Result<HttpClient> CreateAuthorizedClient(CouchDbConnection connection)
    {
        var httpClientResult = _httpClientFactory.CreateClientSafely("CouchDbState", _logger);

        if (httpClientResult.IsFailure)
        {
            _logger.LogError("Не удалось создать HttpClient: {Error}", httpClientResult.Error);
            return Result.Failure<HttpClient>(httpClientResult.Error);
        }

        var httpClient = httpClientResult.Value;
        httpClient.BaseAddress = new Uri(connection.NetAddress);

        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{connection.UserName}:{connection.Password}"));
        httpClient.DefaultRequestHeaders.Authorization = new("Basic", authToken);

        return Result.Success(httpClient);
    }

    private async Task<Result<string>> NodeName(HttpClient httpClient, CancellationToken cancellationToken)
    {
        var responseResult = await httpClient.SendRequestSafelyAsync(
            client => client.GetAsync("/_membership", cancellationToken),
            _logger,
            "получение списка нод CouchDB");

        if (responseResult.IsFailure)
            return Result.Failure<string>(responseResult.Error);

        using var response = responseResult.Value;

        if (!response.IsSuccessStatusCode)
            return Result.Failure<string>($"Не удалось получить список нод CouchDB: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var membership = JsonSerializer.Deserialize<MembershipResponse>(json);
        var nodeName = membership?.AllNodes.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(nodeName))
            return Result.Failure<string>("В ответе /_membership нет нод");

        return Result.Success(nodeName);
    }

    private async Task<Result<string>> LogLevel(HttpClient httpClient, string nodeName, CancellationToken cancellationToken)
    {
        var url = ConfigUrl(nodeName, "log", "level");

        var responseResult = await httpClient.SendRequestSafelyAsync(
            client => client.GetAsync(url, cancellationToken),
            _logger,
            "чтение настройки log/level");

        if (responseResult.IsFailure)
            return Result.Failure<string>(responseResult.Error);

        using var response = responseResult.Value;

        if (response.StatusCode == HttpStatusCode.NotFound)
            return Result.Success(string.Empty);

        if (!response.IsSuccessStatusCode)
            return Result.Failure<string>($"Не удалось прочитать log/level: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var value = JsonSerializer.Deserialize<string>(json) ?? string.Empty;

        return Result.Success(value);
    }

    private async Task<Result> SetLogLevel(HttpClient httpClient, string nodeName, string value, CancellationToken cancellationToken)
    {
        var url = ConfigUrl(nodeName, "log", "level");
        using var content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

        var responseResult = await httpClient.SendRequestSafelyAsync(
            client => client.PutAsync(url, content, cancellationToken),
            _logger,
            "запись настройки log/level");

        if (responseResult.IsFailure)
            return Result.Failure(responseResult.Error);

        using var response = responseResult.Value;

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Не удалось установить log/level в {Value}: {StatusCode} {Body}", value, response.StatusCode, body);
            return Result.Failure($"Не удалось установить log/level: {response.StatusCode}");
        }

        _logger.LogInformation("Для CouchDB установлен log/level = {Value}", value);
        return Result.Success();
    }

    private static string ConfigUrl(string nodeName, string section, string key)
        => $"/_node/{Uri.EscapeDataString(nodeName)}/_config/{section}/{key}";

    private sealed class MembershipResponse
    {
        [JsonPropertyName("all_nodes")]
        public List<string> AllNodes { get; set; } = [];
    }
}
