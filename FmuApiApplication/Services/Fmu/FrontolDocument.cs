using FmuApiDomain.Models.TrueSignApi.MarkData.Check;
using FmuApiDomain.Models.Fmu.Document;
using Microsoft.Extensions.Logging;
using FmuApiApplication.Services.TrueSign;
using CSharpFunctionalExtensions;
using FmuApiSettings;
using FmuApiCouhDb.CrudServices;
using FmuApiCouhDb.DocumentModels;
using FmuApiApplication.Services.Frontol;
using FmuApiDomain.Models.TrueSignApi.MarkData;
using FmuApiDomain.Models.MarkInformation;

namespace FmuApiApplication.Services.Fmu
{
    public class FrontolDocument
    {
        private readonly CheckMarks _checkMarks;
        private readonly MarkInformationCrud _markStateCrud;
        private readonly FrontolDocumentCrud _frontolDocumentCrud;
        private readonly FrontolSprtDataService? _frontolSprtDataService;
        private readonly ILogger<FrontolDocument> _logger;

        public FrontolDocument(CheckMarks checkMarks, MarkInformationCrud markStateCrud, FrontolDocumentCrud frontolDocumentCrud, FrontolSprtDataService frontolSprtDataService, ILogger<FrontolDocument> logger)
        {
            _checkMarks = checkMarks;
            _markStateCrud = markStateCrud;
            _frontolDocumentCrud = frontolDocumentCrud;
            _frontolSprtDataService = frontolSprtDataService;
            _logger = logger;
        }

        public FrontolDocument(CheckMarks checkMarks, MarkInformationCrud markStateCrud, FrontolDocumentCrud frontolDocumentCrud, ILogger<FrontolDocument> logger)
        {
            _checkMarks = checkMarks;
            _markStateCrud = markStateCrud;
            _frontolDocumentCrud = frontolDocumentCrud;
            _logger = logger;
        }

        public async Task<Result<FmuAnswer>> CheckAsync(RequestDocument document)
        {
            // согласно документации к fmu в запросе всегда приходит 1 марка или 1 код маркировки
            if (document.Positions.Count == 1)
            {
                if (document.Positions[0].Marking_codes.Count == 1)
                    return await MarkCheckAsync(document.Positions[0].Marking_codes[0]);
            }

            // для совместимости
            return await MarksCheckAsync(document);
        }

        public async Task<Result<FmuAnswer>> MarkCheckAsync(string markCodeData)
        {
            FmuAnswer answer;
            CheckMarksDataTrueApi markDataFromTrueApi;

            MarkCode mark = MarkCode.Create(markCodeData, _markStateCrud, _checkMarks);
            _logger.LogInformation("Марка для проверки {markCodeData}", mark.Code);

            int organisationId = await WareOrganisationId(mark.Barcode);
            mark.SetPrintGroup(organisationId);
            _logger.LogInformation("Код организации {organisationId}", organisationId);

            (bool markIsOk, answer) = await mark.OfflineCheckAsync();

            if (markIsOk & mark.CodeIsSgtin & mark.DatabaseState().HaveTrueApiAnswer & mark.DatabaseState().State == MarkState.Sold)
                return Result.Success(answer);

            if (!markIsOk)
                return Result.Success(answer);

            markDataFromTrueApi = mark.TrueApiData();

            if (!Constants.Online && markDataFromTrueApi.Codes.Count == 0)
                return Result.Failure<FmuAnswer>("Нет интернета");

            if (!Constants.Online && markDataFromTrueApi.Codes.Count > 0)
                return Result.Success(answer);

            bool onlineCheckResult = await mark.OnlineCheckAsync();

            if (!onlineCheckResult)
            {
                _logger.LogWarning("[{Date}] - Ошибка онлайн проверки кода марки {Code}: {Err}", DateTime.Now, mark.Code, mark.ErrorDescription);
            }

            try
            {
                await mark.Save();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[{Date}] - Ошибка отправки данных марки {Cis} в базу данных. \r\n {err}", DateTime.Now, mark.Code, ex.Message);
            }
            
            answer = new()
            {
                Code = mark.ErrorDescription == string.Empty ? 0 : 1,
                Error = mark.ErrorDescription,
                Truemark_response = mark.TrueApiData(),
            };

            return Result.Success(answer);
        }

        private async Task<int> WareOrganisationId(string barcode)
        {
            if (_frontolSprtDataService == null)
                return 0;

            if (!Constants.Parametrs.FrontolConnectionSettings.ConnectionEnable())
                return 0;

            if (barcode == string.Empty)
                return 0;

            return await _frontolSprtDataService.PrintGroupCodeByBarcodeAsync(barcode);
        }
        
        private async Task<Result<FmuAnswer>> MarksCheckAsync(RequestDocument document)
        {
            FmuAnswer answer = new();

            var fmuDb = Constants.Parametrs.Database;

            Dictionary<string, string> marksForCheck = document.MarkDictionary();

            if (fmuDb.OfflineCheckIsEnabled)
            {
                answer = await OfflineCheckAsync(marksForCheck.Values.ToList());

                if (answer.Truemark_response.Codes.Count > 0)
                {
                    if (answer.AllMarksIsSold())
                        return Result.Success(answer);

                    if (answer.AllMarksIsExpire())
                        return Result.Success(answer);
                }
            }

            if (!Constants.Online)
            {
                if (answer.Truemark_response.Codes.Count == 0)
                    return Result.Failure<FmuAnswer>("Нет интернета");
                else
                    return Result.Success(answer);
            }

            answer = await OnlineCheckMarksAsync(marksForCheck.Values.ToList());

            await SaveMarkDataToDb(answer.Truemark_response);

            return Result.Success(answer);
        }
        private async Task<FmuAnswer> OnlineCheckMarksAsync(List<string> marks)
        {
            FmuAnswer answer = new();

            if (marks.Count == 0)
                return new();

            CheckMarksRequestData checkMarksRequestData = new(marks);

            var trueMarkCheckResult = await _checkMarks.RequestMarkState(checkMarksRequestData);

            answer.Truemark_response = trueMarkCheckResult.IsSuccess ? trueMarkCheckResult.Value : new();
            answer.Error = trueMarkCheckResult.IsFailure ? trueMarkCheckResult.Error : "";

            if (answer.Truemark_response.Code != 0)
                return answer;

            foreach (var trueApiMarkData in answer.Truemark_response.Codes)
            {
                string markError = trueApiMarkData.MarkError();

                trueApiMarkData.Cis = trueApiMarkData.Cis.Replace("\u001d", Convert.ToChar(29).ToString());

                if (markError != string.Empty)
                {
                    answer.Code = 1;
                    answer.Error = markError;
                    answer.Marking_codes.Add(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(trueApiMarkData.Cis)));
                }
            }

            return answer;
        }
        private async Task<FmuAnswer> OfflineCheckAsync(List<string> marks)
        {
            FmuAnswer answer = new();

            if (marks.Count == 0)
                return answer;

            answer.Code = 0;
            answer.Error = "Данные получены в offline режиме";

            foreach (var mark in marks)
            {
                string fmuMarkId = CreateMarkId(mark);

                var tApiData = await _markStateCrud.GetAsync(fmuMarkId);

                if (tApiData.TrueApiAnswerProperties.ReqId == "")
                {
                    return answer;
                }
                else
                {
                    answer.Truemark_response = new()
                    {
                        Code = 0,
                        Description = tApiData.TrueApiAnswerProperties.Description,
                        ReqId = tApiData.TrueApiAnswerProperties.ReqId,
                        ReqTimestamp = tApiData.TrueApiAnswerProperties.ReqTimestamp,
                    };
                }
                // если статус марки продана, то не даем ее повторно продать
                tApiData.TrueApiCisData.Sold = (tApiData.State == MarkState.Sold);

                answer.Truemark_response.Codes.Add(tApiData.TrueApiCisData);
            }

            return answer;
        }
        private async Task SaveMarkDataToDb(CheckMarksDataTrueApi trueMarkResponse)
        {
            if (!Constants.Parametrs.Database.ConfigurationIsEnabled && Constants.Parametrs.Database.MarksStateDbName != string.Empty)
                return;

            foreach (var tApiData in trueMarkResponse.Codes)
            {
                string fmuMarkId = CreateMarkId(tApiData.Cis);

                MarkInformation markState = new()
                {
                    MarkId = fmuMarkId,
                    State = tApiData.Sold ? MarkState.Sold : MarkState.Stock,
                    TrueApiCisData = tApiData,
                    TrueApiAnswerProperties = new()
                    {
                        Code = trueMarkResponse.Code,
                        Description = trueMarkResponse.Description,
                        ReqId = trueMarkResponse.ReqId,
                        ReqTimestamp = trueMarkResponse.ReqTimestamp
                    }
                };

                try
                {
                    await _markStateCrud.AddAsync(markState);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[{Date}] - Ошибка отправки данных марки {Cis} в базу данных. \r\n {err}", DateTime.Now, tApiData.Cis, ex.Message);
                }
            }
        }
        private static string CreateMarkId(string mark)
        {
            string fmuMarkId;

            if (mark.Length == 29) //это маркировка табака без gs
                fmuMarkId = mark.Substring(0, 21);
            else
            {
                if (mark.StartsWith("01"))
                {
                    if (mark.IndexOf(@"\u001d") > 0)
                        mark = mark.Replace(@"\u001d", Convert.ToChar(29).ToString());

                    int gsPos = mark.IndexOf(Convert.ToChar(29));

                    if (gsPos > 0)
                        fmuMarkId = $"{mark.Substring(2, 14)}{mark.Substring(18, gsPos - 18)}";
                    else
                        fmuMarkId = mark;
                    
                }
                else
                    fmuMarkId = mark;
            }

            return fmuMarkId;
        }

        public async Task<Result<FmuAnswer>> BeginDocumentAsync(RequestDocument document)
        {
            FmuAnswer chekResult = new();

            FrontolDocumentData frontolDocument = new()
            {
                Id = document.Uid,
                Document = document
            };
            
            await _frontolDocumentCrud.AddAsync(frontolDocument);

            foreach (var position in document.Positions)
            {
                if (position.Marking_codes.Count == 0)
                    continue;

               foreach (var markB64 in position.Marking_codes)
                {
                    MarkCode mark = await MarkCode.CreateAsync(markB64, _markStateCrud, _checkMarks);

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
                            chekResult.Error +=  $"\r\n {position.Text} цена ниже минимальной розничной!";
                            chekResult.Marking_codes.Add(markB64);
                        }

                        if (markData.Mrp < position.Total_price * 100)
                        {
                            chekResult.Code = 3;
                            chekResult.Error +=  $"\r\n {position.Text} цена выше максимальной розничной!";
                            chekResult.Marking_codes.Add(markB64);
                        }
                    }

                }
            }
            
            return chekResult;
        }

        public async Task<int> CommitDoumentAsync(RequestDocument document)
        {
            FrontolDocumentData frontolDocumentPositions = await _frontolDocumentCrud.GetAsync(document.Uid);

            if (frontolDocumentPositions.Id == string.Empty)
                return 404;

            SaleData saleData = new()
            {
                CheqNumber = frontolDocumentPositions.Document.Number,
                SaleDate = DateTime.Now,
                Pos = frontolDocumentPositions.Document.Pos,
                IsSale = frontolDocumentPositions.Document.Type == "receipt"
            };

            string state = saleData.IsSale ? MarkState.Sold : MarkState.Returned;

            foreach (var position in frontolDocumentPositions.Document.Positions)
            {
                foreach (var markB64 in position.Marking_codes)
                {
                    MarkCode mark = await MarkCode.CreateAsync(markB64, _markStateCrud, _checkMarks);

                    var trueApiData = mark.TrueApiData();

                    if (trueApiData.Codes[0].InGroup(TrueApiGoup.Beer.ToString()) && trueApiData.Codes[0].InnerUnitCount != null)
                    {
                        if (trueApiData.Codes[0].InnerUnitCount - (trueApiData.Codes[0].SoldUnitCount ?? 0) - (position.Quantity) * 1000 > 0)
                            continue;
                    }
            
                    await _markStateCrud.SetStateAsync(mark.SGtin, state, saleData);
                }
            }

            await _frontolDocumentCrud.DelteAsync(document.Uid);

            return 200;
        }

        public async Task<bool> CancelDocumentAsync(RequestDocument document)
        {
            await _frontolDocumentCrud.DelteAsync(document.Uid);
            return true;
        }
    }
}
