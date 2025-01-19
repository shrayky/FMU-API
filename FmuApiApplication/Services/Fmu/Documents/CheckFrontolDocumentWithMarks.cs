using CSharpFunctionalExtensions;
using FmuApiDomain.Cache;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using FmuApiSettings;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;

// Это устаревший метод оставленный тут только для совместимости - вдруг фронтол начнет посылать много марок для проверки

namespace FmuApiApplication.Services.Fmu.Documents
{
    public class CheckFrontolDocumentWithMarks : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkInformationService _markInformationService { get; set; }
        private ICacheService _casCacheService { get; set; }
        private ILogger _logger { get; set; }

        private CheckFrontolDocumentWithMarks(RequestDocument requestDocument, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _casCacheService = cacheService;
            _logger = logger;
        }

        private static CheckFrontolDocumentWithMarks CreateObjext(RequestDocument requestDocument, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            return new CheckFrontolDocumentWithMarks(requestDocument, markInformationService, cacheService, logger);
        }

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IMarkInformationService markInformationService, ICacheService cacheService, ILogger logger)
        {
            return CreateObjext(requestDocument, markInformationService, cacheService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            FmuAnswer answer;

            var marksForCheck = _document.MarkDictionary().Values.ToList();

            answer = await OfflineCheckAsync(marksForCheck);

            if (answer.Truemark_response.Codes.Count > 0)
            {
                if (answer.AllMarksIsSold())
                    return Result.Success(answer);

                if (answer.AllMarksIsExpire())
                    return Result.Success(answer);
            }

            if (!Constants.Online)
            {
                if (answer.Truemark_response.Codes.Count == 0)
                    return Result.Failure<FmuAnswer>("Нет интернета");
                
                return Result.Success(answer);
            }

            answer = await OnlineCheckMarksAsync(marksForCheck);

            return Result.Success(answer);
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

                var tApiData = await _markInformationService.MarkInformationAsync(fmuMarkId);

                if (tApiData.TrueApiAnswerProperties.ReqId == "")
                    continue;

                answer.Truemark_response = new()
                    {
                        Code = 0,
                        Description = tApiData.TrueApiAnswerProperties.Description,
                        ReqId = tApiData.TrueApiAnswerProperties.ReqId,
                        ReqTimestamp = tApiData.TrueApiAnswerProperties.ReqTimestamp,
                    };
                
                // если статус марки продана, то не даем ее повторно продать
                tApiData.TrueApiCisData.Sold = (tApiData.State == MarkState.Sold);

                answer.Truemark_response.Codes.Add(tApiData.TrueApiCisData);
            }

            return answer;
        }

        private async Task<FmuAnswer> OnlineCheckMarksAsync(List<string> marks)
        {
            FmuAnswer answer = new();

            if (marks.Count == 0)
                return new();

            CheckMarksRequestData checkMarksRequestData = new(marks);

            //var trueMarkCheckResult = await _checkMarks.RequestMarkState(checkMarksRequestData);

            //answer.Truemark_response = trueMarkCheckResult.IsSuccess ? trueMarkCheckResult.Value : new();
            //answer.Error = trueMarkCheckResult.IsFailure ? trueMarkCheckResult.Error : "";

            if (answer.Truemark_response.Code != 0)
                return answer;

            foreach (var trueApiMarkData in answer.Truemark_response.Codes)
            {
                string markError = trueApiMarkData.MarkErrorDescription();

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


    }
}
