using CentralServerExchange.Interfaces;
using CSharpFunctionalExtensions;
using FmuApiDomain.Attributes;
using FmuApiDomain.CentralServiceExchange.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.DTO.FmuApiExchangeData.Answer;
using FmuApiDomain.DTO.FmuApiExchangeData.DataPacket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using static CSharpFunctionalExtensions.Result;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class ExchangeActionsService : ICentralServerExchangeActions
{
    private readonly ILogger<ExchangeActionsService> _logger;
    
    private readonly IParametersService _parametersService;
    private readonly IExchangeService _exchangeService;
    private readonly ConfigurationDownloadService _configurationDownloadService;
    private readonly SoftwareUpdateDownloadService _softwareUpdateDownloadService;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string EndppintAddress = "api/FmuApiInstanceMonitoring";

    public ExchangeActionsService(ILogger<ExchangeActionsService> logger, IParametersService parametersService, IExchangeService exchangeService, ConfigurationDownloadService configurationDownloadService, SoftwareUpdateDownloadService softwareUpdateDownloadService, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _parametersService = parametersService;
        _exchangeService = exchangeService;
        _configurationDownloadService = configurationDownloadService;
        _softwareUpdateDownloadService = softwareUpdateDownloadService;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> StartExchange()
    {
        var configuration = await _parametersService.CurrentAsync().ConfigureAwait(false);
        var baseAddress = $"{address}/{EndppintAddress}";

        var data = await CreateDataPacket();

        var exchangeResult = await SendPacket(data, configuration.FmuApiCentralServer.Address);
            
        if (exchangeResult.IsFailure)
            return false;

        await _softwareUpdateDownloadService.DownloadAndInstall(exchangeResult.Value, baseAddress).ConfigureAwait(false);

        await _configurationDownloadService.DownloadAndApply(exchangeResult.Value, baseAddress, configuration.FmuApiCentralServer.Token).ConfigureAwait(false);
            
        return true;
    }

    private async Task<DataPacket> CreateDataPacket()
    {
        using var scope = _scopeFactory.CreateScope();
        var nodeInformationService = scope.ServiceProvider
            .GetRequiredService<INodeInformationService>();

        var data = await nodeInformationService.Create().ConfigureAwait(false);

        return data;
    }

    private async Task<Result<FmuApiCentralResponse>> SendPacket(DataPacket dataPacket, string baseAddress)
    {
        var exchangeResult = await _exchangeService.ActExchange(dataPacket, baseAddress).ConfigureAwait(false);

        if (exchangeResult.IsFailure)
        {
            _logger.LogError("Обмен с центральным сервером завершен с ошибкой: {Error}", exchangeResult.Error);
            return Result.Failure<FmuApiCentralResponse>(exchangeResult.Error);
        }

        return ex;
    }
}