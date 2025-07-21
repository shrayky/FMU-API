using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CouchDb.Workers
{
    class CouchDbStatusWorker : BackgroundService
    {
        private readonly ILogger<CouchDbStatusWorker> _logger;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _applicationState;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

        public CouchDbStatusWorker(ILogger<CouchDbStatusWorker> logger,
            IParametersService parametersService,
            IApplicationState applicationState,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _parametersService = parametersService;
            _applicationState = applicationState;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await CheckCouchState();
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

    }
}
