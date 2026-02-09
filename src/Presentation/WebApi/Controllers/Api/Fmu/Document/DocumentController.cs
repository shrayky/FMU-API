using FmuApiApplication.Documents;
using FmuApiDomain.Fmu.Document;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Api.Fmu.Document
{
    [Route("api/fmu/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol mark unit API")]
    public class DocumentController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly FrontolDocumentServiceFactory _factory;
        
        public DocumentController(
            ILogger<DocumentController> logger, 
            FrontolDocumentServiceFactory factory)
        {
            _logger = logger;
            _factory = factory;
        }

        [HttpPost]
        public async Task<IActionResult> DocumentPostAsync(RequestDocument document)
        {
            var service = _factory.GetInstance(document);

            if (service == null)
            {
                return BadRequest();
            }

            var result = await service.ActionAsync();

            if (result.IsFailure)
                return BadRequest(result.Error);

            return Ok(result.Value);
        }
    }
}
