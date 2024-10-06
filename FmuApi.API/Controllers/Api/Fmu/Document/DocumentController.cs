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

        private readonly bool AlcoUnitIsEnabled = (Constants.Parametrs.FrontolAlcoUnit.NetAdres != string.Empty);
        private readonly bool FrontolDocumentDbIsEnabled = (Constants.Parametrs.Database.FrontolDocumentsDbName!= string.Empty);

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
            if (FrontolDocumentDbIsEnabled)
                await _frontolDocument.CancelDocumentAsync(document);

            if (AlcoUnitIsEnabled)
                await SendDocumentToAlcoUnitAsync(document);

            Constants.LastCheckMarkInformation = new();

            return Ok();
        }

        private async Task<IActionResult> CommitDocumentAsync(RequestDocument document)
        {
            int resultCode = 200;

            if (FrontolDocumentDbIsEnabled) 
            {
                resultCode = await _frontolDocument.CommitDoumentAsync(document);
            }

            if (AlcoUnitIsEnabled)
                await SendDocumentToAlcoUnitAsync(document);

            Constants.LastCheckMarkInformation = new();

            return resultCode == 200 ? Ok() : BadRequest();
        }

        private async Task<IActionResult> BeginDocumentAsync(RequestDocument document)
        {
            FmuAnswer answerData = new();

            if (FrontolDocumentDbIsEnabled)
            {
                var result = await _frontolDocument.BeginDocumentAsync(document);

                if (result.IsSuccess)
                    answerData = result.Value;
            }

            if (document.IsAlcoholCheck() && AlcoUnitIsEnabled)
                await SendDocumentToAlcoUnitAsync(document);

            Constants.LastCheckMarkInformation = new();

            return Ok(answerData);
        }

        private async Task<IActionResult> CheckDocument(RequestDocument document)
        {
            if (document.IsAlcoholCheck() && AlcoUnitIsEnabled)
                return await SendDocumentToAlcoUnitAsync(document);

            return await CheckMarkInDoument(document);
        }

        private async Task<IActionResult> CheckMarkInDoument(RequestDocument document)
        {
            // для документов возврата никаких проверок делать не надо
            // можно сразу возвращать 200
            // если в настройках иного не указано
            if (document.Type == FmuDocumentsTypes.ReceiptReturn && !Constants.Parametrs.SaleControlConfig.CheckReceiptReturn)
                return Ok();

            // фронтол шлет 2 запроса, 2 с штрикодом и серйиным номером, закешируем результат ответа, что 2 раза не опрашивать базу
            // работать будет только с 1 кассой, думаю, нужно на кэш переходить
            if (Constants.LastCheckMarkInformation.SGtin() == document.Mark())
                return Ok(Constants.LastCheckMarkInformation);

            Result<FmuAnswer> markCheckReult;

            try
            {
                markCheckReult = await _frontolDocument.CheckAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[{Date}] - Ошибка проверки документа: {err}", DateTime.Now, ex.Message);

                FmuAnswer answer = new()
                {
                    Code = 0,
                    Error = ex.Message,
                };

                return Ok(answer);
            }

            if (markCheckReult.IsFailure)
            {
                FmuAnswer answer = new()
                {
                    Code = 0,
                    Error = markCheckReult.Error
                };

                return Ok(answer);
            }

            return Ok(markCheckReult.Value);
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
