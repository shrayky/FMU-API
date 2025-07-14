using CouchDb.DocumentModels;
using CSharpFunctionalExtensions;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class BeginDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkService _markInformationService { get; set; }
        private ICacheService _cacheService { get; set; }
        private IParametersService _parametersService { get; set; }
        private ILogger _logger { get; set; }

        private Parameters _configuration;

        private BeginDocument(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _cacheService = cacheService;
            _logger = logger;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        }

        private static BeginDocument CreateObject(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return new BeginDocument(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return CreateObject(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            await SendDocumentToAlcoUnitAsync();

            return await BeginDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> BeginDocumentAsync()
        {
            FmuAnswer checkResult = new();

            FrontolDocumentData frontolDocument = new()
            {
                Id = _document.Uid,
                Document = _document
            };

            foreach (var position in _document.Positions)
            {
                if (position.Marking_codes.Count == 0)
                    continue;

                foreach (var markInBase64 in position.Marking_codes)
                {
                    var mark = await _markInformationService.MarkAsync(markInBase64);

                    CheckMarksDataTrueApi trueApiCisData = await mark.TrueApiData();

                    if (trueApiCisData.Codes.Count == 0)
                        continue;

                    CodeDataTrueApi markData = trueApiCisData.Codes[0];

                    if (markData.GroupIds.Contains(TrueApiGroup.Tobaco))
                    {
                        var minPrice = _configuration.MinimalPrices.Tabaco > markData.Smp ? _configuration.MinimalPrices.Tabaco : markData.Smp;

                        if (minPrice > position.Total_price * 100)
                        {
                            checkResult.Code = 3;
                            checkResult.Error += $"\r\n {position.Text} цена ниже минимальной розничной!";
                            checkResult.Marking_codes.Add(markInBase64);
                        }

                        if (markData.Mrp < position.Total_price * 100)
                        {
                            checkResult.Code = 3;
                            checkResult.Error += $"\r\n {position.Text} цена выше максимальной розничной!";
                            checkResult.Marking_codes.Add(markInBase64);
                        }
                    }

                }
            }

            if (_configuration.Database.ConfigurationIsEnabled)
                await _markInformationService.AddDocumentToDbAsync(frontolDocument);
            else
                _cacheService.Set($"cashDoc_{_document.Uid}", _document, TimeSpan.FromMinutes(5));

            return Result.Success(checkResult);
        }

        private async Task<Result> SendDocumentToAlcoUnitAsync()
        {
            RequestDocument auDoc = _document;

            if (_configuration.FrontolAlcoUnit.NetAdres == string.Empty)
                return Result.Success(auDoc);

            var positionsForDelete = new List<Position>();

            foreach (var pos in auDoc.Positions)
            {
                if (pos.Stamps.Count > 0)
                    continue;

                if (pos.Marking_codes.Count == 0)
                    continue;

                positionsForDelete.Add(pos);
            }

            foreach (var pos in positionsForDelete)
                auDoc.Positions.Remove(pos);

            if (auDoc.Positions.Count == 0)
                return Result.Success();

            return Result.Success();
        }

    }
}
