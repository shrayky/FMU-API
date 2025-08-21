using CSharpFunctionalExtensions;

namespace FmuApiDomain.Fmu.Document.Interface
{
    public interface IFrontolDocumentService
    {
        abstract static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider);
        public Task<Result<FmuAnswer>> ActionAsync();
    }
}
