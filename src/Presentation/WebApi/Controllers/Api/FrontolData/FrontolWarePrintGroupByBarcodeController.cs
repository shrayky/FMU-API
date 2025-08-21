using FmuApiDomain.Frontol.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.FrontolData
{
    [Route("api/frontol/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol database")]
    public class FrontolWarePrintGroupByBarcodeController : Controller
    {
        private readonly ILogger<FrontolWarePrintGroupByBarcodeController> _logger;
        private readonly IFrontolSprTService _frontolSprTService;

        public FrontolWarePrintGroupByBarcodeController(IFrontolSprTService frontolSprtService, ILogger<FrontolWarePrintGroupByBarcodeController> logger)
        {
            _frontolSprTService = frontolSprtService;
            _logger = logger;
        }

        [HttpGet("{barcode}")]
        public async Task<IActionResult> GetAsync(string barcode)
        {
            var printGroup = await _frontolSprTService.PrintGroupCodeByBarcodeAsync(barcode);

            if (printGroup.IsFailure)
            {
                _logger.LogError("Ошибка при получении кода группы печати в базе фронтола: {eMessage}", printGroup.Error);
            }

            return Ok(printGroup.Value);
        }
    }
}
