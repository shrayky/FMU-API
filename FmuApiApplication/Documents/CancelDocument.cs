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
    public class CancelDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ICacheService _cacheService { get; set; }
        IParametersService _parametersService { get; set; }
        private ILogger _logger { get; set; }

        private Parameters _configuration;

        private CancelDocument(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _cacheService = cacheService;
            _logger = logger;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        }

        private static CancelDocument CreateObject
            (RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            ILogger logger)
        {
            return new CancelDocument(requestDocument, markInformationService, cacheService, parametersService, logger);
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
            await SendDocumentToAlcoUnitAsync();

            return await CancelDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CancelDocumentAsync()
        {
            FmuAnswer checkResult = new();

            if (_configuration.Database.FrontolDocumentsDbName == string.Empty)
                return Result.Success(checkResult);

            await _markInformationService.DeleteDocumentFromDbAsync(_document.Uid);

            return Result.Success(checkResult);
        }

        private async Task<Result> SendDocumentToAlcoUnitAsync()
        {
            RequestDocument auDoc = _document;

            if (_configuration.FrontolAlcoUnit.NetAdres == string.Empty)
                return Result.Success(auDoc);

            return Result.Success(auDoc);
        }
    }
}
