using System.Text.Json;
using CentralServerExchange.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.DTO.FmuApiExchangeData.Request;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Shared.Strings;
using TrueApiCdn.Interface;

namespace CentralServerExchange.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class NodeInformationService : INodeInformationService
{
    private readonly IParametersService _parametersService;
    private readonly ICdnService _cdnService;
    private IApplicationState _applicationState;

    public NodeInformationService(IParametersService parametersService, ICdnService cdnService, IApplicationState applicationState)
    {
        _parametersService = parametersService;
        _cdnService = cdnService;
        _applicationState =  applicationState;
    }

    public async Task<DataPacket> Create()
    {
        var settings = await _parametersService.CurrentAsync();        

        Payload packetPayload = new()
        {
            NodeInformation = new NodeInformation(),
            FmuApiSetting = MapSettings(settings),
            CdnInformation = await MapCdn(),
            LocalModuleInformation = MapLocalModules(settings)
        };

        var data = JsonSerializer.Serialize(packetPayload, JsonSerializerOptions.Default);

        if (settings.FmuApiCentralServer.Secret != "")
        {
            data = SecretString.EncryptData(data, settings.FmuApiCentralServer.Secret);
        }

        DataPacket packet = new()
        {
            Token = settings.FmuApiCentralServer.Token,
            Data = data
        };

        return packet;
    }

    private static FmuApiSetting MapSettings(Parameters settings)
    {
        List<Organization> fmuApiOrganizations = [];

        foreach (var printGroup in settings.OrganisationConfig.PrintGroups)
        {
            var organization = new Organization
            {
                Id = printGroup.Id,
                Inn = printGroup.INN,
                Name = printGroup.Name,
                XApiKey = printGroup.XAPIKEY
            };
            
            fmuApiOrganizations.Add(organization);
        }

        return new()
        {
            Version = settings.AppVersion,
            Assembly = settings.Assembly,
            ServerConfiguration = new ServerConfiguration
            {
                ApiIpPort = settings.ServerConfig.ApiIpPort
            },
            HostsToPing = settings.HostsToPing,
            MinimalPrices = new MinimalPrices()
            {
                Tabaco = settings.MinimalPrices.Tabaco
            },
            SaleControl = new SaleControl()
            {
                BanSalesReturnedWares = settings.SaleControlConfig.BanSalesReturnedWares,
                IgnoreVerificationErrorForTrueApiGroups = settings.SaleControlConfig.IgnoreVerificationErrorForTrueApiGroups,
                CheckReceiptReturn = settings.SaleControlConfig.CheckReceiptReturn,
                CorrectExpireDateInSaleReturn = settings.SaleControlConfig.CorrectExpireDateInSaleReturn,
                SendEmptyTrueApiAnswerWhenTimeoutError = settings.SaleControlConfig.SendEmptyTrueApiAnswerWhenTimeoutError,
                CheckIsOwnerField = settings.SaleControlConfig.CheckIsOwnerField,
                SendLocalModuleInformationalInRequestId = settings.SaleControlConfig.SendLocalModuleInformationalInRequestId,
                RejectSalesWithoutCheckInformationFrom = settings.SaleControlConfig.RejectSalesWithoutCheckInformationFrom,
                ResetSoldStatusForReturn = settings.SaleControlConfig.ResetSoldStatusForReturn,
            },
            Organizations = fmuApiOrganizations,
            Database = new Database()
            {
                Enabled = settings.Database.Enable
            },
            TokenService = new TokenService()
            {
                Enabled = settings.TrueSignTokenService.Enable,
                Address = settings.TrueSignTokenService.ConnectionAddress
            },
            TimeOut = new TimeOutConfiguration()
            {
                CdnRequest = settings.HttpRequestTimeouts.CdnRequestTimeout,
                InternetConnectionCheck = settings.HttpRequestTimeouts.CheckInternetConnectionTimeout,
                TrueSignCheckRequest = settings.HttpRequestTimeouts.CheckMarkRequestTimeout
            },
            Logging = new Logging()
            {
                IsEnabled = settings.Logging.IsEnabled,
                LogDepth = settings.Logging.LogDepth,
                LogLevel = settings.Logging.LogLevel,
            }
        };
    }

    private async Task<List<CdnInformation>> MapCdn()
    {
        var cnds = await _cdnService.GetCdnsAsync();

        List<CdnInformation> cdnToUpload = [];

        foreach (var cdn in cnds)
        {
            var cdnData = new CdnInformation()
            {
                Host = cdn.Host,
                Latency = cdn.Latency,
                Offline = cdn.Offline,
                OfflineFrom = cdn.OfflineFrom,
            };
            
            cdnToUpload.Add(cdnData);
        }

        return cdnToUpload;
    }

    private List<LocalModuleInformation> MapLocalModules(Parameters  settings)
    {

        var connectedLocalModules =
            settings.OrganisationConfig.PrintGroups.Where(printGroup => printGroup.LocalModuleConnection.Enable);
        
        List<LocalModuleInformation> localModulesStateToUpload = [];
        
        foreach (var printGroup in connectedLocalModules)
        {
            var state = _applicationState.LocalModuleInformation(printGroup.Id);

            var lmInfo = new LocalModuleInformation()
            {
                Id = printGroup.Id,
                Address = printGroup.LocalModuleConnection.ConnectionAddress,
                Version = state.Version,
                LastSync = state.LastSyncTimestamp,
                OperationMode = state.OperationModeRaw,
                Status = state.StatusRaw
            };
            
            localModulesStateToUpload.Add(lmInfo);

        }

        return localModulesStateToUpload;
    }
}