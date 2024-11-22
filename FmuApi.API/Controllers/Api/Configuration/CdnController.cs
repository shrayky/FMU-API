using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class CdnController : Controller
    {
        [HttpGet]
        public IActionResult CdnList()
        {
            return Ok(Constants.Cdn.List);
        }
    }
}
