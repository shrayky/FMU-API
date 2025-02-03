using CSharpFunctionalExtensions;
using FmuApiDomain.Cache;
using FmuApiDomain.Configuration;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiDomain.Fmu.Document.Interface
{
    public interface IFrontolDocumentService
    {
        abstract static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            ILogger logger);
        public Task<Result<FmuAnswer>> ActionAsync();
    }
}
