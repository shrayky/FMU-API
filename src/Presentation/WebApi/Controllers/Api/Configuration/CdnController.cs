using Microsoft.AspNetCore.Mvc;
using TrueApiCdn.Interface;

namespace WebApi.Controllers.Api.Configuration
{
    [Route("api/configuration/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "App configuration")]
    public class CdnController : Controller
    {
        private readonly ICdnService _cdnService;
        public CdnController(ICdnService cdnService) 
        {
            _cdnService = cdnService;
        }

        [HttpGet]
        public async Task<IActionResult> CdnListAwait()
        {
            return Ok(await _cdnService.GetCdnsAsync());
        }
    }
}
