using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class FrontolDocumentServiceFactory
    {
        private readonly ILogger<FrontolDocumentServiceFactory> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FrontolDocumentServiceFactory(ILogger<FrontolDocumentServiceFactory> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public IFrontolDocumentService? GetInstance(RequestDocument document)
        {
            return document.Action switch
            {
                "check" => FrontolCheckDocumentService(document),
                "begin" => BeginDocument.Create(document, _serviceProvider),
                "commit" => CommitDocument.Create(document, _serviceProvider),
                "cancel" => CancelDocument.Create(document, _serviceProvider),
                _ => null
            };
        }

        private IFrontolDocumentService FrontolCheckDocumentService(RequestDocument document)
        {
            if (document.Mark == string.Empty)
                return CheckFrontolDocumentWithMarks.Create(document, _serviceProvider);

            if (document.Type == FmuDocumentsTypes.ReceiptReturn)
                return CheckReturnDocument.Create(document, _serviceProvider);

            return CheckSellDocument.Create(document, _serviceProvider);
        }

    }
}
