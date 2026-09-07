using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.Documents;
using FmuApiDomain.Frontol;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.Mark.Interfaces;
using FmuApiDomain.ProductGroups.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Mark.Services;

public class MarkFabric(
    ILoggerFactory loggerFactory,
    IMarkParser markParser,
    IMarkChecker markChecker,
    IMarkStateManager markStateManager,
    IParametersService parametersService,
    IFrontolSprTService frontolSprTService,
    IGtinCatalogService gtinCatalogService,
    IProductGroupResolver productGroupResolver) : IMarkFabric
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IMarkParser _markParser = markParser;
    private readonly IMarkChecker _markChecker = markChecker;
    private readonly IMarkStateManager _markStateManager = markStateManager;
    private readonly IParametersService _parametersService = parametersService;
    private readonly IFrontolSprTService _frontolSprTService = frontolSprTService;
    private readonly IGtinCatalogService _gtinCatalogService = gtinCatalogService;
    private readonly IProductGroupResolver _productGroupResolver = productGroupResolver;

    public async Task<IMark> Create(Position position, string mark)
    {
        var logger = _loggerFactory.CreateLogger<Mark>();

        var markInstance = new Mark(mark, _markParser, _markChecker, _markStateManager, _gtinCatalogService, _parametersService, logger);

        var appSettings = await _parametersService.CurrentAsync();

        var inn = position.Organisation?.Inn ?? string.Empty;
        var printGroupCode = await SetOrganizationId(markInstance, appSettings.OrganisationConfig.PrintGroups, inn);

        await SetProductGroup(markInstance, position);

        SetTsPiotSettings(markInstance, position, appSettings, printGroupCode);

        return markInstance;
    }

    private async Task SetProductGroup(Mark markInstance, Position position)
    {
        var itemType = position.ItemType;
        int? groupId;

        if (itemType > 0)
        {
            groupId = await _productGroupResolver.Resolve(itemType, string.Empty);
            markInstance.SetPositionData(itemType, position.Text, groupId ?? 0);
            return;
        }

        groupId = await _productGroupResolver.Resolve(0, markInstance.Gtin);

        if (!groupId.HasValue)
        {
            var wareTypeResult = await _frontolSprTService.WareTypeByBarcodeAsync(markInstance.Barcode);

            if (wareTypeResult.IsSuccess && FrontolWareType.IsMarked(wareTypeResult.Value))
            {
                itemType = wareTypeResult.Value;
                groupId = await _productGroupResolver.Resolve(itemType, string.Empty);
            }
        }

        markInstance.SetPositionData(itemType, position.Text, groupId ?? 0);
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

        if (pgCode == 0)
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
