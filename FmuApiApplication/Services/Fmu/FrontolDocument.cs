using FmuApiDomain.Models.TrueSignApi.MarkData.Check;
using FmuApiDomain.Models.Fmu.Document;
using Microsoft.Extensions.Logging;
using FmuApiApplication.Services.TrueSign;
using CSharpFunctionalExtensions;
using FmuApiDomain.Models.MarkState;
using FmuApiCouhDb.CrudServices;
using FmuApiSettings;

namespace FmuApiApplication.Services.Fmu
{
    public class FrontolDocument
    {
        private readonly CheckMarks _checkMarks;
        private readonly MarkStateCrud _markStateCrud;
        private readonly ILogger<FrontolDocument> _logger;

        public FrontolDocument(CheckMarks checkMarks, MarkStateCrud markStateCrud, ILogger<FrontolDocument> logger)
        {
            _checkMarks = checkMarks;
            _markStateCrud = markStateCrud;
            _logger = logger;
        }

        public async Task<Result<AnswerDocument>> CheckAsync(RequestDocument document)
        {
            AnswerDocument answer = new();
            Dictionary<string, string> marksForCheck = document.MarkDictionary();

            if (Constants.Parametrs.MarksDb.ConfigurationEnabled())
                answer = await OfflineCheckAsync(marksForCheck);

            if (answer.Truemark_response.Codes.Count > 0)
            {
                if (answer.AllMarksIsSold())
                    return answer;

                if (answer.AllMarksIsExpire())
                    return answer;
            }

            if (!Constants.Online)
            {
                if (answer.Truemark_response.Codes.Count == 0)
                    return Result.Failure<AnswerDocument>("Нет интернета");
                else
                    return Result.Success(answer);
            }

            try
            {
                answer = await CheckMarksAsync(marksForCheck);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[{Date}] - Ошибка проверки документа: {err}", DateTime.Now, ex.Message);

                return Result.Failure<AnswerDocument>(ex.Message);
            }

            await SaveMarkDataToDb(answer.Truemark_response);

            return Result.Success(answer);
        }

        private async Task<AnswerDocument> CheckMarksAsync(Dictionary<string, string> marksForCheck)
        {
            AnswerDocument answer = new();

            if (marksForCheck.Count == 0)
                return new();

            CheckMarksRequestData checkMarksRequestData = new(marksForCheck.Values.ToList());

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

        private async Task SaveMarkDataToDb(CheckAnswerTrueApi trueMarkResponse)
        {
            if (!Constants.Parametrs.MarksDb.ConfigurationEnabled())
                return;

            foreach (var tApiData in trueMarkResponse.Codes)
            {
                MarkState markState = new()
                {
                    MarkId = tApiData.Cis,
                    State = tApiData.Sold ? "sold" : "stock",
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

        private async Task<AnswerDocument> OfflineCheckAsync(Dictionary<string, string> marksForCheck)
        {
            AnswerDocument answer = new();

            if (marksForCheck.Count == 0)
                return answer;

            answer.Code = 0;
            answer.Error = "Данные получены в offline режиме";

            foreach (var mark in marksForCheck)
            {
                var tApiData = await _markStateCrud.GetAsync(mark.Value);

                answer.Marking_codes.Add(mark.Key);
                answer.Truemark_response = new()
                {
                    Code = 0,
                    Description = tApiData.TrueApiAnswerProperties.Description,
                    ReqId = tApiData.TrueApiAnswerProperties.ReqId,
                    ReqTimestamp = tApiData.TrueApiAnswerProperties.ReqTimestamp,
                };

                answer.Truemark_response.Codes.Add(tApiData.TrueApiCisData);

            }

            return answer;
        }

    }
}
