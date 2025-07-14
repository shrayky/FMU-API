using CSharpFunctionalExtensions;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CancelDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkService _markInformationService { get; set; }
        private ICacheService _cacheService { get; set; }
        IParametersService _parametersService { get; set; }
        private ILogger _logger { get; set; }

        private Parameters _configuration;

        private CancelDocument(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
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
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return new CancelDocument(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return CreateObject(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            await SendDocumentToAlcoUnitAsync();

            return await CancelDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CancelDocumentAsync()
        {
            FmuAnswer checkResult = new();

            if (!_configuration.Database.ConfigurationIsEnabled)
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
