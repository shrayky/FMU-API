using CentralServerExchange.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.CentralServiceExchange.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class ExchangeActionsService : ICentralServerExchangeActions
{
    private readonly ILogger<ExchangeActionsService> _logger;
    
    private readonly IParametersService _parametersService;
    private readonly INodeInformationService _nodeInformationService;
    private readonly IExchangeService _exchangeService;
    private readonly ConfigurationDownloadService _configurationDownloadService;
    private readonly SoftwareUpdateDownloadService _softwareUpdateDownloadService;

    public ExchangeActionsService(ILogger<ExchangeActionsService> logger, IParametersService parametersService, INodeInformationService nodeInformationService, IExchangeService exchangeService, ConfigurationDownloadService configurationDownloadService, SoftwareUpdateDownloadService softwareUpdateDownloadService)
    {
        _logger = logger;
        _parametersService = parametersService;
        _nodeInformationService = nodeInformationService;
        _exchangeService = exchangeService;
        _configurationDownloadService = configurationDownloadService;
        _softwareUpdateDownloadService = softwareUpdateDownloadService;
    }
    
    public async Task<bool> StartExchange()
    {
        var configuration = await _parametersService.CurrentAsync().ConfigureAwait(false);
        var data = await _nodeInformationService.Create().ConfigureAwait(false);
        var baseAddress = $"{configuration.FmuApiCentralServer.Address}/api/FmuApiInstanceMonitoring";
            
        var exchangeResult = await _exchangeService.ActExchange(data, baseAddress).ConfigureAwait(false);

        if (exchangeResult.IsFailure)
        {
            _logger.LogError("Обмен с центральным сервером завершен с ошибкой: {Error}", exchangeResult.Error);
            return false;
        }

        await _softwareUpdateDownloadService.DownloadAndInstall(exchangeResult.Value, baseAddress).ConfigureAwait(false);
        await _configurationDownloadService.DownloadAndApply(exchangeResult.Value, baseAddress, configuration.FmuApiCentralServer.Token).ConfigureAwait(false);
            
        return true;
    }
}