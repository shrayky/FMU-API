using CouchDb.DocumentModels;
using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueSignApi.MarkData;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using FmuApiSettings;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.Fmu.Documents
{
    public class BeginDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ILogger _logger { get; set; }

        private BeginDocument(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _logger = logger;
        }

        private static BeginDocument CreateObjext(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            return new BeginDocument(requestDocument, markInformationService, logger);
        }

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IMarkInformationService markInformationService, ILogger logger)
        {
            return CreateObjext(requestDocument, markInformationService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            Constants.LastCheckMarkInformation = new();

            await SendDocumentToAlcoUnitAsync();

            return await BeginDocumentAsync();

        }

        private async Task<Result<FmuAnswer>> BeginDocumentAsync()
        {
            FmuAnswer chekResult = new();

            if (Constants.Parametrs.Database.FrontolDocumentsDbName == string.Empty)
                return Result.Success(chekResult);

            FrontolDocumentData frontolDocument = new()
            {
                Id = _document.Uid,
                Document = _document
            };

            await _markInformationService.AddDocumentToDbAsync(frontolDocument);

            foreach (var position in _document.Positions)
            {
                if (position.Marking_codes.Count == 0)
                    continue;

                foreach (var markInbase64 in position.Marking_codes)
                {
                    var mark = await _markInformationService.MarkAsync(markInbase64);

                    CheckMarksDataTrueApi trueApiCisData = mark.TrueApiData();

                    if (trueApiCisData.Codes.Count == 0)
                        continue;

                    CodeDataTrueApi markData = trueApiCisData.Codes[0];

                    if (markData.GroupIds.Contains(TrueApiGoup.Tobaco))
                    {
                        var minPrice = Constants.Parametrs.MinimalPrices.Tabaco > markData.Smp ? Constants.Parametrs.MinimalPrices.Tabaco : markData.Smp;

                        if (minPrice > position.Total_price * 100)
                        {
                            chekResult.Code = 3;
                            chekResult.Error += $"\r\n {position.Text} цена ниже минимальной розничной!";
                            chekResult.Marking_codes.Add(markInbase64);
                        }

                        if (markData.Mrp < position.Total_price * 100)
                        {
                            chekResult.Code = 3;
                            chekResult.Error += $"\r\n {position.Text} цена выше максимальной розничной!";
                            chekResult.Marking_codes.Add(markInbase64);
                        }
                    }

                }
            }

            return Result.Success(chekResult);

        }

        private async Task<Result> SendDocumentToAlcoUnitAsync()
        {
            RequestDocument auDoc = _document;

            if (Constants.Parametrs.FrontolAlcoUnit.NetAdres == string.Empty)
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
