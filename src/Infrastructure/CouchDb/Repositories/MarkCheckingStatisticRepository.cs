using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Database.Dto;
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
                SuccessCheck = false,
                WarningMessage = warningMessage
            };

            await CreateAsync(entity);
        }

    }
}
