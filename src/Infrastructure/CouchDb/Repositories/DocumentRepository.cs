using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Frontol;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace CouchDb.Repositories
{
    public class DocumentRepository : BaseCouchDbRepository<DocumentEntity>, IDocumentRepository
    {
        public DocumentRepository(ILogger<DocumentRepository> logger, CouchDbContext context, IParametersService appConfiguration, IApplicationState applicationState) : base(logger, context, context.Documents, appConfiguration, applicationState)
        {
        }
        
        public async Task<Result<DocumentEntity>> Add(RequestDocument document)
        {
            if (_context == null)
                return Result.Failure<DocumentEntity>("Не подключена БД");

            DocumentEntity entity = new()
            {
                Id = document.Uid,
                FrontolDocument = document
            };

            await CreateAsync(entity);

            return Result.Success<DocumentEntity>(entity);
        }

        public async Task<Result<bool>> Delete(RequestDocument document)
        {
            if (_context == null)
                return Result.Failure<bool>("Не подключена БД");

            await DeleteAsync(document.Uid);

            return Result.Success(true);
        }

        public async Task<Result<bool>> Delete(string uid)
        {
            if (_context == null)
                return Result.Failure<bool>("Не подключена БД");

            await DeleteAsync(uid);

            return Result.Success(true);
        }

        public async Task<Result<DocumentEntity>> Get(string id)
        {
            if (_context == null)
                return Result.Failure<DocumentEntity>("Не подключена БД");

            var data = await base.GetByIdAsync(id);

            if (data == null)
                return Result.Failure<DocumentEntity>($"Не найден документ с uid {id}");

            return Result.Success(data);
        }
    }
}
