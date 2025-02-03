using FmuApiDomain.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApi.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IParametersService _parametersService;

        [BindProperty]
        public int ApiServerIpPort { get; set; } = 2578;

        public IndexModel(ILogger<IndexModel> logger, IParametersService parametersService)
        {
            _logger = logger;
            _parametersService = parametersService;

            ApiServerIpPort = _parametersService.Current().ServerConfig.ApiIpPort;
        }

        public void OnGet()
        {

        }
    }
}
