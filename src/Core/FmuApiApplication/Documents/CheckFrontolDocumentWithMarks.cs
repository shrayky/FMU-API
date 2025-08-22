using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Это устаревший метод оставленный тут только для совместимости - вдруг фронтол начнет посылать много марок для проверки

namespace FmuApiApplication.Documents
{
    public class CheckFrontolDocumentWithMarks : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkStateManager _markStateService { get; set; }
        private IParametersService _parametersService { get; set; }
        private IApplicationState _applicationState { get; set; }
        private ILogger<CheckFrontolDocumentWithMarks> _logger { get; set; }

        private Parameters _configuration;

        private CheckFrontolDocumentWithMarks(RequestDocument requestDocument, IServiceProvider provider)
        {
            _document = requestDocument;

            _markStateService = provider.GetRequiredService<IMarkStateManager>();
            _parametersService = provider.GetRequiredService<IParametersService>();
            _applicationState = provider.GetRequiredService<IApplicationState>();
            _logger = provider.GetRequiredService<ILogger<CheckFrontolDocumentWithMarks>>();

            _configuration = _parametersService.Current();
        }

        private static CheckFrontolDocumentWithMarks CreateObject(RequestDocument requestDocument, IServiceProvider provider)
            => new(requestDocument, provider);
            
        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
            => CreateObject(requestDocument, provider);

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

            if (!_applicationState.IsOnline())
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
                
                //var tApiData = await _markInformationService.MarkInformationAsync(fmuMarkId);
                var tApiData = await _markStateService.Information(fmuMarkId);

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
                tApiData.TrueApiCisData.Sold = tApiData.State == MarkState.Sold;

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
