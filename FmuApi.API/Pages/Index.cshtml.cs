using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApi.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public int ApiServerIpPort { get; set; } = 2578;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            ApiServerIpPort = Constants.Parametrs.ServerConfig.ApiIpPort;
        }

        public void OnGet()
        {

        }
    }
}
