using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.Fmu
{
    [Route("api/fmu")]
    [ApiController]
    [ApiExplorerSettings (IgnoreApi =  true)]
    public class DefaultFmuController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Redirect("~/swagger");
        }
    }
}
