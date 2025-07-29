using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Frontol;

namespace FmuApiDomain.Repositories
{
    public interface IDocumentRepository
    {
        Task<Result<DocumentEntity>> Get(string uid);
        Task<Result<DocumentEntity>> Add(RequestDocument document);
        Task<Result<bool>> Delete(RequestDocument document);
        Task<Result<bool>> Delete(string uid);
    }
}