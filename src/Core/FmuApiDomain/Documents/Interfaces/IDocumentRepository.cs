using CSharpFunctionalExtensions;
using FmuApiDomain.Documents.Entities;
using FmuApiDomain.Documents;

namespace FmuApiDomain.Documents.Interfaces;

public interface IDocumentRepository
{
    Task<Result<DocumentEntity>> Get(string uid);
    Task<Result<DocumentEntity>> Add(RequestDocument document);
    Task<Result<bool>> Delete(RequestDocument document);
    Task<Result<bool>> Delete(string uid);
}