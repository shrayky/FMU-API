using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using FmuApiDomain.TrueApiIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrueApi.Interface;

namespace FmuApiApplication.Mark.Services;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class MarksCheckService : IOnLineMarkCheckService
{
    private readonly ILogger<MarksCheckService> _logger;
    private readonly IApplicationState _applicationState;
    private readonly ITrueApiClientService _api;

    private readonly int requestTimeoutSeconds = 2;
    private readonly int requestAttempts = 1;

    private Parameters _configuration;

    public MarksCheckService(ILogger<MarksCheckService> logger, IParametersService parametersService, IApplicationState applicationState, ITrueApiClientService api)
    {
        _logger = logger;
        _applicationState = applicationState;

        _configuration = parametersService.Current();
        requestTimeoutSeconds = _configuration.HttpRequestTimeouts.CheckMarkRequestTimeout;

        _api = api;
    }

    public Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, int organizationCode)
    {
        var group = _configuration.OrganisationConfig.PrintGroup(organizationCode);
        return RequestWithCredential(marks, group, organizationCode);
    }

    public Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, PrintGroupData group)
        => RequestWithCredential(marks, group, group.Id);

    public Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks)
        => RequestMarkState(marks, 0);

    private async Task<Result<CheckMarksDataTrueApi>> RequestWithCredential(
        CheckMarksRequestData marks,
        PrintGroupData? group,
        int organizationCode)
    {
        var trueApiToken = group == null
            ? string.Empty
            : _applicationState.TrueApiToken(group.INN).Token;
        var xApiKey = XApiKeyCredential.Resolve(group, trueApiToken);

        if (string.IsNullOrEmpty(xApiKey))
            return Result.Failure<CheckMarksDataTrueApi>($"Не получен XAPIKEY для организации с кодом {organizationCode}");

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
