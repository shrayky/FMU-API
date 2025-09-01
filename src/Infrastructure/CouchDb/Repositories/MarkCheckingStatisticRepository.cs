using CouchDB.Driver.Extensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories
{
    public class MarkCheckingStatisticRepository : BaseCouchDbRepository<StatisticEntity>, ICheckStatisticRepository
    {
        public MarkCheckingStatisticRepository(ILogger<MarkCheckingStatisticRepository> logger, CouchDbContext context, IParametersService appConfiguration, IApplicationState applicationState) : base(logger, context, context.MarkCheckingStatistic, appConfiguration, applicationState)
        {

        }

        public async Task FailureCheck(string mark, DateTime checkDate)
        {
            StatisticEntity entity = new()
            {
                Id = $"{mark}_{checkDate}",
                checkDate = checkDate,
                SGtin = mark,
                OnLineCheck = false,
                OffLineCheck = false,
                SuccessCheck = false
            };

            await CreateAsync(entity);
        }

        public async Task SuccessOffLineCheck(string mark, DateTime checkDate)
        {
            StatisticEntity entity = new()
            {
                Id = $"{mark}_{checkDate}",
                checkDate = checkDate,
                SGtin = mark,
                OnLineCheck = false,
                OffLineCheck= true,
                SuccessCheck = true,
                WarningMessage = ""
            };

            await CreateAsync(entity);
        }

        public async Task OffLineCheckWithWarnings(string mark, DateTime checkDate, string warningMessage)
        {
            StatisticEntity entity = new()
            {
                Id = $"{mark}_{checkDate}",
                checkDate = checkDate,
                SGtin = mark,
                OnLineCheck = false,
                OffLineCheck = true,
                SuccessCheck = false,
                WarningMessage = warningMessage
            };

            await CreateAsync(entity);
        }

        public async Task SuccessOnLineCheck(string mark, DateTime checkDate)
        {
            StatisticEntity entity = new()
            {
                Id = $"{mark}_{checkDate}",
                checkDate = checkDate,
                SGtin = mark,
                OnLineCheck = true,
                OffLineCheck = false,
                SuccessCheck = true
            };

            await CreateAsync(entity);
        }

        public async Task OnLineCheckWithWarnings(string mark, DateTime checkDate, string warningMessage)
        {
            StatisticEntity entity = new()
            {
                Id = $"{mark}_{checkDate}",
                checkDate = checkDate,
                SGtin = mark,
                OnLineCheck = true,
                OffLineCheck = false,
                SuccessCheck = false,
                WarningMessage = warningMessage
            };

            await CreateAsync(entity);
        }

        public async Task<MarkCheckStatistics> CheckStatisticsByDays(DateTime fromDate, DateTime toDate)
        {
            if (_context == null)
                return new();

            if (!_appState.CouchDbOnline())
                return new();

            var appConfig = await _appConfiguration.CurrentAsync();
            var queryLimit = appConfig.Database.QueryLimit == 0 ? 1000000 : appConfig.Database.QueryLimit;

            var filteredMarks = await _database
                .Where(p => p.Data.checkDate >= fromDate && p.Data.checkDate <= toDate)
                .Take(queryLimit)
                .ToListAsync();

            var statistics = new MarkCheckStatistics
            {
                Total = filteredMarks.Count,
                SuccessfulOnlineChecks = filteredMarks.Count(m => m.Data.SuccessCheck && m.Data.OnLineCheck),
                SuccessfulOfflineChecks = filteredMarks.Count(m => m.Data.SuccessCheck && m.Data.OffLineCheck)
            };

            return statistics;
        }
    }
}
