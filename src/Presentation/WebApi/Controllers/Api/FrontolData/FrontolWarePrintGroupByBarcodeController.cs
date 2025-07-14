using FmuApiDomain.Configuration.Interfaces;
using FrontolDb.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.FrontolData
{
    [Route("api/frontol/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol database")]
    public class FrontolWarePrintGroupByBarcodeController : Controller
    {
        private readonly ILogger<FrontolWarePrintGroupByBarcodeController> _logger;
        private readonly FrontolSprtDataHandler _frontolSprtDataHandler;
        private readonly IParametersService _parametersService;

        public FrontolWarePrintGroupByBarcodeController(FrontolSprtDataHandler frontolSprtDataHandler, IParametersService parametersService, ILogger<FrontolWarePrintGroupByBarcodeController> logger)
        {
            _frontolSprtDataHandler = frontolSprtDataHandler;
            _parametersService = parametersService;
            _logger = logger;
        }

        [HttpGet("{barcode}")]
        public async Task<IActionResult> GetAsync(string barcode)
        {
            var appParams = _parametersService.Current();

            if (appParams.OrganisationConfig.PrintGroups.Count <= 1)
                return Ok(0);

            if (!appParams.FrontolConnectionSettings.ConnectionEnable())
                return Ok(0);

            var printGroup = await _frontolSprtDataHandler.PrintGroupCodeByBarcodeAsync(barcode);

            if (printGroup.IsFailure)
            {
                _logger.LogError("Ошибка при получении кода группы печати в базе фронтола: {eMessage}", printGroup.Error);
            }

            return Ok(printGroup.Value);
        }
    }
}
