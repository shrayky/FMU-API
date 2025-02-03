using CSharpFunctionalExtensions;
using FmuApiDomain.Cache;
using FmuApiDomain.Configuration;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueSignApi.MarkData;
using FmuApiSettings;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CommitDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ICacheService _cacheService { get; set; }
        private ILogger _logger { get; set; }
        private IParametersService _parametersService { get; set; }

        private Parameters _configuration;
        const string saleDocumentType = "receipt";

        private CommitDocument(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _cacheService = cacheService;
            _parametersService = parametersService;
            _logger = logger;

            _configuration = parametersService.Current();
        }

        private static CommitDocument CreateObject(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            ILogger logger)
        {
            return new CommitDocument(requestDocument, markInformationService, cacheService, parametersService, logger);
        }

        public static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkInformationService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            ILogger logger)
        {
            return CreateObject(requestDocument, markInformationService, cacheService, parametersService, logger);
        }
        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            await SendDocumentToAlcoUnitAsync();

            return await CommitDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CommitDocumentAsync()
        {
            FmuAnswer checkResult = new();

            if (_configuration.Database.FrontolDocumentsDbName == string.Empty)
                return Result.Success(checkResult);

            IFrontolDocumentData frontolDocument = await _markInformationService.DocumentFromDbAsync(_document.Uid);

            if (frontolDocument.Id == string.Empty)
                return Result.Failure<FmuAnswer>($"Невозможно закрыть документ {_document.Uid}! Он не найден в базе документов!");

            SaleData saleData = new()
            {
                CheqNumber = frontolDocument.Document.Number,
                SaleDate = DateTime.Now,
                Pos = frontolDocument.Document.Pos,
                IsSale = frontolDocument.Document.Type == saleDocumentType
            };

            string state = saleData.IsSale ? MarkState.Sold : MarkState.Returned;

            foreach (var position in frontolDocument.Document.Positions)
            {
                foreach (string markInBase64 in position.Marking_codes)
                {
                    var mark = await _markInformationService.MarkAsync(markInBase64);

                    var trueApiData = mark.TrueApiData();

                    if (trueApiData.Codes[0].InGroup(TrueApiGoup.Beer.ToString()) && trueApiData.Codes[0].InnerUnitCount != null)
                    {
                        if (trueApiData.Codes[0].InnerUnitCount - (trueApiData.Codes[0].SoldUnitCount ?? 0) - position.Quantity * 1000 > 0)
                            continue;
                    }

                    await _markInformationService.MarkChangeState(mark.SGtin, state, saleData);
                }
            }

            await _markInformationService.DeleteDocumentFromDbAsync(_document.Uid);

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
