using FmuApiApplication.Services.Fmu.Documents;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.MarkInformation.Interfaces;
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
        private readonly IMarkInformationService _markInformationService;

        public DocumentController(ILogger<DocumentController> logger, FrontolDocumentServiceFactory factory, IMarkInformationService markInformationService)
        {
            _logger = logger;
            _factory = factory;
            _markInformationService = markInformationService;
        }

        [HttpPost]
        async public Task<IActionResult> DocumentPostAsync(RequestDocument document)
        {
            var service = _factory.GetInstance(document, _markInformationService, _logger);

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
