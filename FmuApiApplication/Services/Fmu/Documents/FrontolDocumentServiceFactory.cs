using FmuApiDomain.Cache;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Fmu.Documents
{
    public class FrontolDocumentServiceFactory
    {
        public IFrontolDocumentService? GetInstance(RequestDocument document, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            return document.Action switch
            {
                "check" => FrontolCheckDocumentService(document, markInformationService, cacheService, logger),
                "begin" => BeginDocument.Create(document, markInformationService, cacheService, logger),
                "commit" => CommitDocument.Create(document, markInformationService, cacheService, logger),
                "cancel" => CancelDocument.Create(document, markInformationService, cacheService, logger),
                _ => null
            };
        }

        private IFrontolDocumentService FrontolCheckDocumentService(RequestDocument document, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            if (document.Mark() == string.Empty)
                return CheckFrontolDocumentWithMarks.Create(document, markInformationService, cacheService, logger);

            if (document.Type == FmuDocumentsTypes.ReceiptReturn)
                return CheckReturnDocument.Create(document, markInformationService, cacheService, logger);

            return CheckSellDocument.Create(document, markInformationService, cacheService, logger);
        }

    }
}
