using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Fmu.Documents
{
    public class FrontolDocumentServiceFactory
    {
        public IFrontolDocumentService? GetInstance(RequestDocument document, IMarkInformationService markInformationService, ILogger logger)
        {
            return document.Action switch
            {
                "check" => FrontolCheckDocumentService(document, markInformationService, logger),
                "begin" => BeginDocument.Create(document, markInformationService, logger),
                "commit" => CommitDocument.Create(document, markInformationService, logger),
                "cancel" => CancelDocument.Create(document, markInformationService, logger),
                _ => null
            };
        }

        private IFrontolDocumentService FrontolCheckDocumentService(RequestDocument document, IMarkInformationService markInformationService, ILogger logger)
        {
            if (document.Mark() == string.Empty)
                return CheckFrontolDocumentWithMarks.Create(document, markInformationService, logger);

            if (document.Type == FmuDocumentsTypes.ReceiptReturn)
                return CheckReturnDocument.Create(document, markInformationService, logger);

            return CheckSellDocument.Create(document, markInformationService, logger);
        }

    }
}
