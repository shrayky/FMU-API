using CSharpFunctionalExtensions;

namespace FmuApiDomain.Documents.Interfaces
{
    public interface IFrontolDocumentService
    {
        abstract static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider);
        public Task<Result<FmuAnswer>> ActionAsync();
    }
}
