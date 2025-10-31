using FmuApiDomain.Configuration.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApi.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IParametersService _parametersService;

        [BindProperty]
        public int ApiServerIpPort { get; set; }
        
        [BindProperty]
        public string NodeName { get; set; }
        
        public IndexModel(IParametersService parametersService)
        {
            _parametersService = parametersService;

            var parameters = _parametersService.Current();
            
            ApiServerIpPort = parameters.ServerConfig.ApiIpPort;
            NodeName = parameters.NodeName;
        }

        public void OnGet()
        {

        }
    }
}
