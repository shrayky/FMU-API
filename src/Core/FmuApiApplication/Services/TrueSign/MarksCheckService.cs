using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrueApi.Interface;

namespace FmuApiApplication.Services.TrueSign
{
    [AutoRegisterService(ServiceLifetime.Scoped)]
    public class MarksCheckService : IOnLineMarkCheckService
    {
        private readonly int requestTimeoutSeconds = 2;
        private readonly int requestAttempts = 1;

        private readonly ILogger<MarksCheckService> _logger;
        private readonly IApplicationState _applicationState;
        private readonly ITrueApiClientService _api;

        private Parameters _configuration;

        public MarksCheckService(ILogger<MarksCheckService> logger, IParametersService parametersService, IApplicationState applicationState, ITrueApiClientService api)
        {
            _logger = logger;
            _applicationState = applicationState;

            _configuration = parametersService.Current();
            requestTimeoutSeconds = _configuration.HttpRequestTimeouts.CheckMarkRequestTimeout;

            _api = api;
        }

        public async Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, int organizationCode)
        {
            string xApiKey;

            if (organizationCode == 0)
                xApiKey = _configuration.OrganisationConfig.XapiKey() ?? "";
            else
                xApiKey = _configuration.OrganisationConfig.XapiKey(organizationCode) ?? "";

            if (string.IsNullOrEmpty(xApiKey))
                return Result.Failure<CheckMarksDataTrueApi>($"Не получен XAPIKEY для организации с кодом {organizationCode}");

            return await DoRequest(marks, xApiKey!);
        }

        public async Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks)
        {
            string xApiKey = _configuration.OrganisationConfig.XapiKey();

            return await DoRequest(marks, xApiKey);
        }

        private async Task<Result<CheckMarksDataTrueApi>> DoRequest(CheckMarksRequestData marks, string xApiKey)
        {
            if (!_applicationState.IsOnline())
                return Result.Failure<CheckMarksDataTrueApi>("Нет интернета");

            var haveActiveCdns = await _api.HaveActiveCdns();

            if (haveActiveCdns.IsFailure)
                return Result.Failure<CheckMarksDataTrueApi>(haveActiveCdns.Error);

            int attemptLost = requestAttempts;

            while (attemptLost > 0)
            {
                var result = await _api.MarksOnLineCheck(marks, xApiKey, TimeSpan.FromSeconds(requestTimeoutSeconds));

                if (result.IsSuccess)
                    return result.Value;

                attemptLost--;
            }

            return Result.Failure<CheckMarksDataTrueApi>("Ни один CDN сервер не ответил.");
        }
    }
}
