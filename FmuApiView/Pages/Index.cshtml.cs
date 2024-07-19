using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FmuApiView.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public string ApiServerAdres { get; set; } = string.Empty;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
            ApiServerAdres = $"http://localhost:{Constants.Parametrs.ServerConfig.ApiIpPort}/api";
        }

        public void OnGet()
        {

        }
    }
}
