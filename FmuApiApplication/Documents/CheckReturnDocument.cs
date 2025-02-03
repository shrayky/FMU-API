using CSharpFunctionalExtensions;
using FmuApiDomain.Cache;
using FmuApiDomain.Configuration;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiSettings;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckReturnDocument : IFrontolDocumentService
    {
        private RequestDocument Document { get; set; }
        private IMarkInformationService MarkInformationService { get; set; }
        private IFrontolDocumentService CheckService { get; set; }
        private ICacheService CacheService { get; set; }
        IParametersService _parametersService { get; set; }
        private ILogger Logger { get; set; }

        private Parameters _configuration;

        private CheckReturnDocument(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService, 
            ILogger logger)
        {
            Document = requestDocument;
            MarkInformationService = markInformationService;
            CacheService = cacheService;
            Logger = logger;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();

            CheckService = CheckSellDocument.Create(Document, MarkInformationService, cacheService, _parametersService, Logger);
        }

        private static CheckReturnDocument CreateObject(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService, 
            ILogger logger)
        {
            return new CheckReturnDocument(requestDocument, markInformationService, cacheService, parametersService, logger);
        }

        public static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService, 
            ILogger logger)
        {
            return CreateObject(requestDocument, markInformationService, cacheService, parametersService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            FmuAnswer answer = new();

            // фронтол 20.5 не требовал проверки марок для документов возврата,
            // начиная с 22.4 такая проверка обязательна
            if (!_configuration.SaleControlConfig.CheckReceiptReturn)
                return Result.Success(answer);

            var checkResult = await CheckService.ActionAsync();

            if (checkResult.IsFailure)
                return checkResult;

            answer = checkResult.Value;

            // фронтол показывает ошибку, если статус марки продан, даже при возврате!
            // Приходится, вот так некрасиво, исправлять.
            answer.Truemark_response.MarkCodeAsNotSaled();

            // зачем нам анализировать поля с ошибками при возврате...
            answer.Truemark_response.ResetErrorFields();

            // фронтол зачем то проверяет срок годности при возврате, поменяем дату
            answer.Truemark_response.CorrectExpireDate();

            return Result.Success(answer);

        }

    }
}
