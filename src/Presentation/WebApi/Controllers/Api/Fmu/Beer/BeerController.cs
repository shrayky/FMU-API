using FmuApiDomain.Fmu.Document;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Fmu.Beer
{
    [Route("api/fmu/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol mark unit API")]
    public class BeerController : Controller
    {
        [HttpPost("/beer/connect_keg")]
        [HttpPost("/api/fmu/beer/connect_keg")]
        public IActionResult ConnectKeg()
        {
            FmuAnswer answer = new();
            return Ok(answer);
        }
    }
}
