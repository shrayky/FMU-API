using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Frontol.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.FrontolData;

/// <summary>
/// Розничная цена товара по штрихкоду из выбранной базы Frontol.
/// </summary>
[Route("api/frontol/[controller]")]
[ApiController]
[ApiExplorerSettings(GroupName = "Frontol database")]
public class WarePriceByBarcodeController : Controller
{
    private readonly IFrontolSprTService _frontolSprTService;
    private readonly IParametersService _parametersService;
    private readonly ILogger<WarePriceByBarcodeController> _logger;

    public WarePriceByBarcodeController(
        IFrontolSprTService frontolSprtService,
        IParametersService parametersService,
        ILogger<WarePriceByBarcodeController> logger)
    {
        _frontolSprTService = frontolSprtService;
        _parametersService = parametersService;
        _logger = logger;
    }

    [HttpGet("{barcode}")]
    public async Task<IActionResult> Get(string barcode)
    {
        var parameters = await _parametersService.CurrentAsync();
        var connectionId = parameters.ConnectedFrontolSettings.ResolveWareDataSourceId();

        if (connectionId == 0)
            return Ok(new { price = (decimal?)null });

        var connection = parameters.ConnectedFrontolSettings.ConnectionSettings
            .FirstOrDefault(item => item.Id == connectionId);

        if (connection is null || !connection.ConnectionEnable())
            return Ok(new { price = (decimal?)null });

        var price = await _frontolSprTService.RetailPriceByBarcode(barcode);

        if (price.IsFailure)
        {
            _logger.LogError("Ошибка при получении цены товара {Barcode} в базе Frontol: {Error}", barcode, price.Error);

            return StatusCode(500, new { error = price.Error });
        }

        return Ok(new { price = price.Value });
    }
}
