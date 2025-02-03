using FmuApiDomain.Cache;
using FmuApiDomain.Configuration;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class FrontolDocumentServiceFactory
    {
        private readonly ILogger<FrontolDocumentServiceFactory> _logger;
        private readonly IParametersService _parametersService;
        private readonly IMarkInformationService _markInformationService;
        private readonly ICacheService _cacheService;

        public FrontolDocumentServiceFactory(
            ILogger<FrontolDocumentServiceFactory> logger,
            IParametersService parametersService, 
            IMarkInformationService markInformationService,
            ICacheService cacheService)
        {
            _logger = logger;
            _parametersService = parametersService;
            _markInformationService = markInformationService;
            _cacheService = cacheService;
        }

        public IFrontolDocumentService? GetInstance(RequestDocument document)
        {
            return document.Action switch
            {
                "check" => FrontolCheckDocumentService(document, _markInformationService, _cacheService, _parametersService, _logger),
                "begin" => BeginDocument.Create(document, _markInformationService, _cacheService, _parametersService, _logger),
                "commit" => CommitDocument.Create(document, _markInformationService, _cacheService, _parametersService, _logger),
                "cancel" => CancelDocument.Create(document, _markInformationService, _cacheService, _parametersService, _logger),
                _ => null
            };
        }

        private IFrontolDocumentService FrontolCheckDocumentService(
            RequestDocument document,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            ILogger logger)
        {
            if (document.Mark() == string.Empty)
                return CheckFrontolDocumentWithMarks.Create(document, markInformationService, cacheService, parametersService, logger);

            if (document.Type == FmuDocumentsTypes.ReceiptReturn)
                return CheckReturnDocument.Create(document, markInformationService, cacheService, parametersService, logger);

            return CheckSellDocument.Create(document, markInformationService, cacheService, parametersService, logger);
        }

    }
}
