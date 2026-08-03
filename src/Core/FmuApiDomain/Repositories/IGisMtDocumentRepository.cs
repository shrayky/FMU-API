using FmuApiDomain.GisMt.Entities;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.Repositories;

public interface IGisMtDocumentRepository
{
    Task<GisMtDocumentEntity?> Get(string id);

    Task<bool> Exists(string id);

    Task<bool> Save(GisMtDocumentEntity entity);
}
