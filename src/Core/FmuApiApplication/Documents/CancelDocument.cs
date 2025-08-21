using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.Documents
{
    public class CancelDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }

        private Lazy<ILogger<CancelDocument>> _logger { get; set; }
        private Lazy<ITemporaryDocumentsService> _temporaryDocumentsService { get; set; }

        private IParametersService _parametersService { get; set; }
        private IApplicationState  _appState { get; set; }

        private Parameters _configuration;

        private CancelDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            _document = requestDocument;

            _temporaryDocumentsService = new Lazy<ITemporaryDocumentsService>(() => provider.GetRequiredService<ITemporaryDocumentsService>());
            _logger = new Lazy<ILogger<CancelDocument>>(() => provider.GetRequiredService<ILogger<CancelDocument>>());
            
            _parametersService = provider.GetRequiredService<IParametersService>();
            _appState = provider.GetRequiredService<IApplicationState>();
            _configuration = _parametersService.Current();
        }

        private static CancelDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
            => new(requestDocument, provider);

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
            => CreateObject(requestDocument, provider);

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            await SendDocumentToAlcoUnitAsync();

            return await CancelDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CancelDocumentAsync()
        {
            FmuAnswer checkResult = new();

            if (!_configuration.Database.ConfigurationIsEnabled)
                return Result.Success(checkResult);

            if (!_appState.CouchDbOnline())
                return Result.Success(checkResult);

            await _temporaryDocumentsService.Value.DeleteDocumentFromDbAsync(_document.Uid);

            return Result.Success(checkResult);
        }

        private async Task<Result> SendDocumentToAlcoUnitAsync()
        {
            RequestDocument auDoc = _document;

            if (_configuration.FrontolAlcoUnit.NetAdres == string.Empty)
                return Result.Success(auDoc);

            return Result.Success(auDoc);
        }
    }
}
