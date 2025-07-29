using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace CouchDb.Workers
{
    class CouchDbStatusWorker : BackgroundService
    {
        private readonly ILogger<CouchDbStatusWorker> _logger;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _applicationState;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CouchDbContext _couchDbContext;

        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

        public CouchDbStatusWorker(IServiceProvider services)
        {
            _logger = services.GetRequiredService<ILogger<CouchDbStatusWorker>>();
            _parametersService = services.GetRequiredService<IParametersService>();
            _applicationState = services.GetRequiredService<IApplicationState>();
            _httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
            _couchDbContext = services.GetRequiredService<CouchDbContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await CheckCouchState();
                await EnsureDatabasesExists();
            }
        }

        private async Task CheckCouchState()
        {
            var _config = await _parametersService.CurrentAsync();
            var database = _config.Database;

            if (!database.ConfigurationIsEnabled)
                return;

            using var httpClient = _httpClientFactory.CreateClient("CouchDbState");

            httpClient.BaseAddress = new Uri(database.NetAddress);

            bool curState = _applicationState.CouchDbOnline();
            bool newSate = false;

            try
            {
                var response = await httpClient.GetAsync("");

                newSate = response.IsSuccessStatusCode;

            }
            catch (Exception e)
            {
                newSate = false;
            }

            if (curState == newSate)
                return;

            if (!newSate)
                _logger.LogError(
                            "Изменение online CouchDb: {curState} -> {newSate}",
                            curState,
                            newSate);
            else
                _logger.LogError("Изменение online CouchDb: {curState} -> {newSate}",
                            curState,
                            newSate);

            _applicationState.UpdateCouchDbState(newSate);
        }

        private async Task EnsureDatabasesExists()
        {
            var config = await _parametersService.CurrentAsync();
            var database = config.Database;

            if (!database.ConfigurationIsEnabled)
                return;

            if (!_applicationState.CouchDbOnline())
                return;

            using var httpClient = _httpClientFactory.CreateClient("CouchDbState");
            httpClient.BaseAddress = new Uri(database.NetAddress);

            var authToken = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{database.UserName}:{database.Password}"));
            httpClient.DefaultRequestHeaders.Authorization = new ("Basic", authToken);

            foreach (var dbName in DatabaseNames.Names())
            {
                var checkResponse = await httpClient.GetAsync($"/{dbName}");

                if (checkResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    continue;
                }

                var createResponse = await httpClient.PutAsync($"/{dbName}", null);

                if (createResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("База данных {DatabaseName} успешно создана", dbName);
                }
                else
                {
                    _logger.LogError("Не удалось создать базу данных {DatabaseName}: {StatusCode}", dbName, createResponse.StatusCode);
                }
            }
        }

    }
}
