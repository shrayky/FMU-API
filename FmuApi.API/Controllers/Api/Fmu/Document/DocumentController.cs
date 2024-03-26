using CSharpFunctionalExtensions;
using FmuApiApplication.Services.AcoUnit;
using FmuApiApplication.Services.Fmu;
using FmuApiDomain.Models.Fmu.Document;
using FmuApiSettings;
using Microsoft.AspNetCore.Mvc;

namespace FmuApiAPI.Controllers.Api.Fmu.Document
{
    [Route("api/fmu/[controller]")]
    [Route("[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "Frontol mark unit API")]
    public class DocumentController : ControllerBase
    {
        private readonly FrontolDocument _frontolDocument;
        private readonly AlcoUnitGateway _alcoUnitGateway;
        private readonly ILogger<DocumentController> _logger;
        private bool AlcoUnitIsEnabled = (Constants.Parametrs.FrontolAlcoUnit.NetAdres != string.Empty);


        public DocumentController(FrontolDocument frontolDocument, AlcoUnitGateway alcoUnitGateway, ILogger<DocumentController> logger)
        {
            _frontolDocument = frontolDocument;
            _alcoUnitGateway = alcoUnitGateway;
            _logger = logger;
        }

        [HttpPost]
        async public Task<IActionResult> DocumentPostAsync(RequestDocument document)
        {
            _logger.LogInformation("Получен документ {@document}", document);

            return document.Action switch
            {
                "check" => await CheckDocument(document),
                "begin" => await BeginDocumentAsync(document),
                "commit" => await CommitDocumentAsync(document),
                "cancel" => await CancelDocumentAsync(document),
                _ => BadRequest($"Not supported action \"{document.Action}\".")
            };
        }

        private async Task<IActionResult> CancelDocumentAsync(RequestDocument document)
        {
            if (document.IsAlcoholCheck() && AlcoUnitIsEnabled)
                return await SendDocumentToAlcoUnitAsync(document);

            return Ok();
        }

        private async Task<IActionResult> CommitDocumentAsync(RequestDocument document)
        {
            if (document.IsAlcoholCheck() && AlcoUnitIsEnabled)
                return await SendDocumentToAlcoUnitAsync(document);

            return Ok();
        }

        private async Task<IActionResult> BeginDocumentAsync(RequestDocument document)
        {
            if (document.IsAlcoholCheck() && AlcoUnitIsEnabled)
                return await SendDocumentToAlcoUnitAsync(document);

            return Ok();
        }

        private async Task<IActionResult> CheckDocument(RequestDocument document)
        {
            if (document.IsAlcoholCheck() && AlcoUnitIsEnabled)
                return await SendDocumentToAlcoUnitAsync(document);

            return await CheckMarkInDoument(document);
        }

        private async Task<IActionResult> CheckMarkInDoument(RequestDocument document)
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

        private async Task<IActionResult> SendDocumentToAlcoUnitAsync(RequestDocument document)
        {
            string answer;

            var positionsForDelete = new List<Position>();

            foreach (var pos in document.Positions)
            {
                if (pos.Stamps.Count > 0)
                    continue;

                if (pos.Marking_codes.Count == 0)
                    continue;

                positionsForDelete.Add(pos);
            }

            foreach (var pos in positionsForDelete)
            {
                document.Positions.Remove(pos);
            }

            try
            {
                answer = await _alcoUnitGateway.SendDDocumentAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ошибка отправки запроса в alco unit {err}", ex);
                return BadRequest(ex.Message);
            }

            _logger.LogInformation("Получено ответ от alcounit {@answer}", answer);

            return Ok(answer);
        }
    }
}
