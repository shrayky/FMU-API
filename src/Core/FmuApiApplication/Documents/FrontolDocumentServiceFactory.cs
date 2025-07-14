using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class FrontolDocumentServiceFactory
    {
        private readonly ILogger<FrontolDocumentServiceFactory> _logger;
        private readonly IParametersService _parametersService;
        private readonly IMarkService _markInformationService;
        private readonly ICacheService _cacheService;
        private readonly IApplicationState _applicationStateService;

        public FrontolDocumentServiceFactory(
            ILogger<FrontolDocumentServiceFactory> logger,
            IParametersService parametersService, 
            IMarkService markInformationService,
            ICacheService cacheService,
            IApplicationState applicationStateService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _markInformationService = markInformationService;
            _cacheService = cacheService;
            _applicationStateService = applicationStateService;
        }

        public IFrontolDocumentService? GetInstance(RequestDocument document)
        {
            return document.Action switch
            {
                "check" => FrontolCheckDocumentService(document, _markInformationService, _cacheService, _parametersService, _applicationStateService,_logger),
                "begin" => BeginDocument.Create(document, _markInformationService, _cacheService, _parametersService, _applicationStateService, _logger),
                "commit" => CommitDocument.Create(document, _markInformationService, _cacheService, _parametersService, _applicationStateService, _logger),
                "cancel" => CancelDocument.Create(document, _markInformationService, _cacheService, _parametersService, _applicationStateService, _logger),
                _ => null
            };
        }

        private IFrontolDocumentService FrontolCheckDocumentService(
            RequestDocument document,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            if (document.Mark == string.Empty)
                return CheckFrontolDocumentWithMarks.Create(document, markInformationService, cacheService, parametersService, applicationStateService, logger);

            if (document.Type == FmuDocumentsTypes.ReceiptReturn)
                return CheckReturnDocument.Create(document, markInformationService, cacheService, parametersService, applicationStateService, logger);

            return CheckSellDocument.Create(document, markInformationService, cacheService, parametersService, _applicationStateService, logger);
        }

    }
}
