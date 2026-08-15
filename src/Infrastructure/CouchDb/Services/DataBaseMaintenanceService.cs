using CouchDb.DatabaseScheme;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Http;
using System.Text;

namespace CouchDb.Services;

public class DataBaseMaintenanceService
{
    private readonly ILogger<DataBaseMaintenanceService> _logger;
    private readonly IApplicationState _appState;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersService _parametersService;

    public DataBaseMaintenanceService(
        ILogger<DataBaseMaintenanceService> logger,
        IApplicationState appState,
        IHttpClientFactory httpClientFactory,
        IParametersService parametersService)
    {
        _logger = logger;
        _appState = appState;
        _httpClientFactory = httpClientFactory;
        _parametersService = parametersService;
    }

    public async Task<bool> CompactDatabase()
    {
        if (!_appState.CouchDbOnline())
            return false;

        var connection = (await _parametersService.CurrentAsync()).Database;
        var httpClientResult = _httpClientFactory.CreateClientSafely("CouchDbState", _logger);

        if (httpClientResult.IsFailure)
        {
            _logger.LogError("Ошибка сжатия БД: {err}", httpClientResult.Error);
            return false;
        }

        using var httpClient = httpClientResult.Value;
        httpClient.BaseAddress = new Uri(connection.NetAddress);

        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{connection.UserName}:{connection.Password}"));
        httpClient.DefaultRequestHeaders.Authorization = new("Basic", authToken);

        var allSucceeded = true;

        foreach (var databaseName in DatabaseNames.Names())
        {
            if (!await CompactSingleDatabase(httpClient, databaseName))
                allSucceeded = false;
        }

        return allSucceeded;
    }

    private async Task<bool> CompactSingleDatabase(HttpClient httpClient, string databaseName)
    {
        try
        {
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync($"/{databaseName}/_compact", content);

            if (response.IsSuccessStatusCode)
                return true;

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Ошибка сжатия БД {DatabaseName}: {StatusCode} {Body}", databaseName, response.StatusCode, body);
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError("Ошибка сжатия БД {DatabaseName}: {err}", databaseName, e.Message);
            return false;
        }
    }
}
