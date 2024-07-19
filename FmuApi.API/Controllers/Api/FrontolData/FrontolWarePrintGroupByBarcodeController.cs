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

        public FrontolWarePrintGroupByBarcodeController(FrontolSprtDataService frontolSprtDataService)
        {
            _frontolSprtDataService = frontolSprtDataService;
        }

        [HttpGet("{barcode}")]
        public async Task<IActionResult> GetAsync(string barcode)
        {
            if (!Constants.Parametrs.FrontolConnectionSettings.ConnectionEnable())
                return Ok(0);

            int printGroup = await _frontolSprtDataService.PrintGroupCodeByBarcodeAsync(barcode);

            return Ok(printGroup);
        }
    }
}
