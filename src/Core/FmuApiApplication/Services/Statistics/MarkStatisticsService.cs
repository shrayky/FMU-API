using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Statistics
{
    [AutoRegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped)]
    public class MarkStatisticsService : IMarkStatisticsService
    {
        private readonly ILogger<MarkStatisticsService> _logger;
        private readonly ICheckStatisticRepository _repository;
        private readonly IParametersService _parametersService;

        private readonly Parameters _parameters;

        public MarkStatisticsService(ILogger<MarkStatisticsService> logger, ICheckStatisticRepository repository, IParametersService parametersService)
        {
            _logger = logger;
            _repository = repository;
            _parametersService = parametersService;

            _parameters = parametersService.Current();
        }

        public async Task<MarkCheckStatistics> ByDays(DateTime fromDate, DateTime toDate)
        {
            if (!_parameters.Database.Enable)
                return new MarkCheckStatistics();
            
            var statistics = await _repository.CheckStatisticsByDays(fromDate, toDate);

            return statistics;
        }

        public async Task<MarkCheckStatistics> LastWeek()
        {
            var toDate = DateTime.Now.Date.AddDays(-1);
            var fromDate = DateTime.Now.AddDays(-8).Date;

            return await ByDays(fromDate, toDate);
        }
        
        public async Task<MarkCheckStatistics> LastMonth()
        {
            var toDate = DateTime.Now.Date.AddDays(-1);
            var fromDate = DateTime.Now.AddDays(-31).Date;

            return await ByDays(fromDate, toDate);
        }

        public async Task<MarkCheckStatistics> Today()
        {
            var toDate = DateTime.Now.Date.AddDays(1);
            var fromDate = DateTime.Today;

            return await ByDays(fromDate, toDate);
        }
    }
}