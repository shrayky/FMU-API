using CSharpFunctionalExtensions;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiDomain.Fmu.Document.Interface
{
    public interface IFrontolDocumentService
    {
        abstract static IFrontolDocumentService Create(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger);
        public Task<Result<FmuAnswer>> ActionAsync();
    }
}
