using CSharpFunctionalExtensions;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.Fmu.Document;

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