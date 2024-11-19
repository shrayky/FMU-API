using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using FmuApiSettings;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Fmu.Documents
{
    public class CheckReturnDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ILogger _logger { get; set; }
        private IFrontolDocumentService _checktService { get; set; }

        private CheckReturnDocument(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _logger = logger;

            _checktService = CheckSellDocument.Create(_document, _markInformationService, _logger);
        }

        private static CheckReturnDocument CreateObjext(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            return new CheckReturnDocument(requestDocument, markInformationService, logger);
        }

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            return CreateObjext(requestDocument, markInformationService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            FmuAnswer answer = new();

            // фронтол 20.5 не требовал проверки марок для документов возврата,
            // начиная с 22.4 такая проверка обязательна
            if (!Constants.Parametrs.SaleControlConfig.CheckReceiptReturn)
                return Result.Success(answer);

            var checkResult = await _checktService.ActionAsync();

            if (checkResult.IsFailure)
                return checkResult;

            answer = checkResult.Value;

            // фронтол выбадет ошибку, если статус марки продан, даже при возврате!
            // Приходится, вот так некрасиво, исправлять.
            answer.Truemark_response.MarkCodeAsNotSaled();

            // зачем нам анализировать поля с ошибками при возварте...
            answer.Truemark_response.ResetErrorFields();

            // фронтол зачем то провереряет срок годности при возврате, поменяем дату
            answer.Truemark_response.CorrectExpireDate();

            return Result.Success(answer);

        }

    }
}
