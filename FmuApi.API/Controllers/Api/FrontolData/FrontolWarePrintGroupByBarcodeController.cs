using FmuApiApplication.Services.Frontol;
using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.FrontolData
{
    [Route("api/frontol/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol database")]
    public class FrontolWarePrintGroupByBarcodeController : Controller
    {
        private readonly FrontolSprtDataService _frontolSprtDataService;
        private readonly ILogger<FrontolWarePrintGroupByBarcodeController> _logger;

        public FrontolWarePrintGroupByBarcodeController(FrontolSprtDataService frontolSprtDataService, ILogger<FrontolWarePrintGroupByBarcodeController> logger)
        {
            _frontolSprtDataService = frontolSprtDataService;
            _logger = logger;
        }

        [HttpGet("{barcode}")]
        public async Task<IActionResult> GetAsync(string barcode)
        {
            if (!Constants.Parametrs.FrontolConnectionSettings.ConnectionEnable())
                return Ok(0);

            var printGroup = await _frontolSprtDataService.PrintGroupCodeByBarcodeAsync(barcode);

            if (printGroup.IsFailure)
            {
                _logger.LogError("Ошибка при получении кода группы печати в базе фронтола: {eMessage}", printGroup.Error);
            }
                
            return Ok(printGroup.Value);
        }
    }
}
