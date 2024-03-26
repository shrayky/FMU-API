using Microsoft.AspNetCore.Mvc;
using FmuApiApplication.Services.AcoUnit;

namespace FmuApiAPI.Controllers.Api.Fmu.Products_by_stamp
{
    [Route("api/fmu/products_by_stamp")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol mark unit API")]
    public class ProductsByStampController : ControllerBase
    {
        private AlcoUnitGateway _alcoUnitGateway;

        public ProductsByStampController(AlcoUnitGateway alcoUnitGateway)
        {
            _alcoUnitGateway = alcoUnitGateway;
        }

        [HttpGet("{stamp}")]
        public async  Task<IActionResult> GetAsync(string stamp) 
        {
            string answer = string.Empty;

            try
            {
                answer = await _alcoUnitGateway.ProductsByStamp(stamp);
                return Ok(answer);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
