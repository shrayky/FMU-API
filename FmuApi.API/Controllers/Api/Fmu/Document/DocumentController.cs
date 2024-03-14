using CSharpFunctionalExtensions;
using FmuApiApplication;
using FmuApiApplication.Services.Fmu;
using FmuApiDomain.Models.Fmu.Document;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.Fmu.Document
{
    [Route("api/fmu/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol mark unit API")]
    public class DocumentController : ControllerBase
    {
        private readonly FrontolDocument _frontolDocument;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(FrontolDocument frontolDocument, ILogger<DocumentController> logger)
        {
            _frontolDocument = frontolDocument;
            _logger = logger;
        }

        [HttpPost]
        async public Task<IActionResult> DocumentPostAsync(RequestDocument document) =>
            document.Action switch
            {
                "check" => await CheckDocument(document),
                _ => BadRequest($"No action like this \"{document.Action}\".")
            };

        private async Task<IActionResult> CheckDocument(RequestDocument document)
        {
            Result<AnswerDocument> result = await _frontolDocument.CheckAsync(document);

            if (result.IsSuccess)
                return Ok(result.Value);

            if (result.IsFailure)
            {
                AnswerDocument answer = new()
                {
                    Code = 0,
                    Error = result.Error
                };

                return Ok(answer);
            }

            return BadRequest();

        }
    }
}
