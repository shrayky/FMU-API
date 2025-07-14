using CouchDB.Driver.Extensions;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CouchDb.Workers.DatabaseMigrationWorkers
{
    class CouchDbMigrationTo102Worker : BackgroundService
    {
        private readonly ILogger<CouchDbMigrationTo102Worker> _logger;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _applicationState;
        private readonly CouchDbContext _dbContext;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);

        private readonly int dataBatchSize = 50000;

        public CouchDbMigrationTo102Worker(
            ILogger<CouchDbMigrationTo102Worker> logger,
            IParametersService parametersService,
            IApplicationState applicationState,
            CouchDbContext couchDbContext,
            IServiceScopeFactory serviceScopeFactory
            )
        {
            _logger = logger;
            _parametersService = parametersService;
            _applicationState = applicationState;
            _dbContext = couchDbContext;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var appParameters = await _parametersService.CurrentAsync();

            if (string.IsNullOrEmpty(appParameters.Database.MarksStateDbName) &&
                string.IsNullOrEmpty(appParameters.Database.FrontolDocumentsDbName))
                return;

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(_checkInterval, stoppingToken);

                appParameters = await _parametersService.CurrentAsync();

                if (!appParameters.Database.ConfigurationIsEnabled ||
                    !_applicationState.CouchDbOnline())
                    continue;

                try
                {
                    var emptyDb = await MigrateMarkDataAsync(dataBatchSize, appParameters);

                    if (emptyDb) {
                        appParameters.Database.MarksStateDbName = string.Empty;
                        appParameters.Database.FrontolDocumentsDbName = string.Empty;
                        await _parametersService.UpdateAsync(appParameters);
                    }
                }
                catch (Exception E)
                {
                    _logger.LogError("Ошибка миграции данных марок на версию 10.2 {err}", E.Message);
                }
            }
        }

        private async Task<bool> MigrateMarkDataAsync(int batchSize, Parameters parametersService)
        {
            if (string.IsNullOrEmpty(parametersService.Database.MarksStateDbName))
                return true;

            var marks = await _dbContext.MarksState.Take(batchSize)
                                                   .ToListAsync();

            if (marks.Count == 0)
            {
                return true;
            }

            _logger.LogWarning("Начинаю перенос {markCount} в новую базу", marks.Count);
            var startTime = DateTime.Now;

            using var scope = _serviceScopeFactory.CreateScope();
            var markInformationRepository = scope.ServiceProvider.GetRequiredService<IMarkInformationRepository>();

            var markEntities = marks.Select(markData => new MarkEntity
            {
                Id = markData.Id,
                MarkId = markData.Id,
                State = markData.State,
                TrueApiCisData = markData.TrueApiInformation,
                TrueApiAnswerProperties = markData.TrueApiAnswerProperties,
                SaleData = markData.SaleInforamtion
            }).ToList();

            bool successAdd = await markInformationRepository.AddRangeAsync(markEntities);

            _logger.LogWarning("Удаляю {markCount} марок из старой базы", marks.Count);

            if (successAdd)
                await _dbContext.MarksState.DeleteRangeAsync(marks);

            var endTime = DateTime.Now;
            var duration = endTime - startTime;

            _logger.LogWarning("Перенос и удаление {markCount} марок выполнены за {duration}",
                marks.Count,
                duration.ToString(@"hh\:mm\:ss\.fff"));

            scope.Dispose();

            return false;
        }

    }
}
