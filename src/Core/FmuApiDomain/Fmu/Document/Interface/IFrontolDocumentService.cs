using CSharpFunctionalExtensions;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiDomain.Fmu.Document.Interface
{
    public interface IFrontolDocumentService
    {
        abstract static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger);
        public Task<Result<FmuAnswer>> ActionAsync();
    }
}
