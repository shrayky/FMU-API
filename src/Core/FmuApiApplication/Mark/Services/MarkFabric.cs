using System.Reflection.Metadata;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Frontol.DTO;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Mark.Services;

public class MarkFabric : IMarkFabric
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMarkParser _markParser;
    private readonly IMarkChecker _markChecker;
    private readonly IMarkStateManager  _markStateManager;
    private readonly IParametersService  _parametersService;
    private readonly IFrontolSprTService _frontolSprTService;

    public MarkFabric(ILoggerFactory loggerFactory, IMarkParser markParser, IMarkChecker markChecker, IMarkStateManager markStateManager, IParametersService parametersService, IFrontolSprTService frontolSprTService)
    {
        _loggerFactory = loggerFactory;
        _markParser = markParser;
        _markChecker = markChecker;
        _markStateManager = markStateManager;
        _parametersService = parametersService;
        _frontolSprTService = frontolSprTService;
    }

    public async Task<IMark> Create(Position position, string mark)
    {
        var logger = _loggerFactory.CreateLogger<Mark>();
        
        var markInstance = new Mark(mark, _markParser, _markChecker, _markStateManager, _parametersService, logger);

        var appSettings = await _parametersService.CurrentAsync();
        
        var inn = position.Organisation?.Inn ?? string.Empty;
        var printGroupCode= await SetOrganizationId(markInstance, appSettings.OrganisationConfig.PrintGroups, inn);

        SetTsPiotSettings(markInstance, position, appSettings, printGroupCode);
        
        return markInstance;
    }

    private static void SetTsPiotSettings(Mark markInstance, Position position, Parameters appSettings, int printGroupCode)
    {
        if (!appSettings.ServerConfig.TsPiotEnabled)
            return;
        
        if (!string.IsNullOrEmpty(position.TsPiot.Host) && !string.IsNullOrEmpty(position.TsPiot.Port))
        {
            markInstance.SetTsPiotSettings(position.TsPiot);
            return;
        }

        var printGroups = appSettings.OrganisationConfig.PrintGroups;
        var printGroup = printGroups.FirstOrDefault(f => f.Id == printGroupCode);
        
        if (printGroup == null)
            return;
        
        var tsPiotSettings = printGroup.TsPiot;
        
        if (!string.IsNullOrEmpty(tsPiotSettings?.Host) && !string.IsNullOrEmpty(tsPiotSettings.Port))
        {
            markInstance.SetTsPiotSettings(tsPiotSettings);
        } 
    }

    private async Task<int> SetOrganizationId(IMark mark, List<PrintGroupData> printGroups, string inn)
    {
        if (printGroups.Count == 1)
        {
            mark.SetPrintGroupCode(printGroups[0].Id);
            return printGroups[0].Id;
        }

        var pgCode = 0;

        if (!string.IsNullOrEmpty(inn))
        {
            var organisation = printGroups.FirstOrDefault(p => p.INN == inn);

            if (organisation != null)
                pgCode = organisation.Id;
        }
        else
        {
            var result = await _frontolSprTService.PrintGroupCodeByBarcodeAsync(mark.Barcode);

            if (result.IsSuccess)
                pgCode = result.Value;
        }

        if (pgCode == 0)
            return 0;

        mark.SetPrintGroupCode(pgCode);
        
        return pgCode;
    }
}