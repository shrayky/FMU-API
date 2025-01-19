using CSharpFunctionalExtensions;
using FmuApiDomain.Cache;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiSettings;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Fmu.Documents
{
    public class CancelDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ICacheService _cacheService { get; set; }
        private ILogger _logger { get; set; }
        private CancelDocument(RequestDocument requestDocument, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _cacheService = cacheService;
            _logger = logger;
        }

        private static CancelDocument CreateObjext(RequestDocument requestDocument, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            return new CancelDocument(requestDocument, markInformationService, cacheService, logger);
        }

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            return CreateObjext(requestDocument, markInformationService, cacheService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            Constants.LastCheckMarkInformation = new();

            await SendDocumentToAlcoUnitAsync();

            return await CancelDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CancelDocumentAsync()
        {
            FmuAnswer chekResult = new();

            if (Constants.Parametrs.Database.FrontolDocumentsDbName == string.Empty)
                return Result.Success(chekResult);

            await _markInformationService.DeleteDocumentFromDbAsync(_document.Uid);

            return Result.Success(chekResult);
        }

        private async Task<Result> SendDocumentToAlcoUnitAsync()
        {
            RequestDocument auDoc = _document;

            if (Constants.Parametrs.FrontolAlcoUnit.NetAdres == string.Empty)
                return Result.Success(auDoc);

            return Result.Success(auDoc);
        }
    }
}
