using FmuApiDomain.Models.TrueSignApi.MarkData.Check;
using FmuApiDomain.Models.Fmu.Document;
using Microsoft.Extensions.Logging;
using FmuApiApplication.Services.TrueSign;
using CSharpFunctionalExtensions;

namespace FmuApiApplication.Services.Fmu
{
    public class FrontolDocument
    {
        private readonly CheckMarks _checkMarks;
        private readonly ILogger<FrontolDocument> _logger;

        public FrontolDocument(CheckMarks checkMarks, ILogger<FrontolDocument> logger)
        {
            _checkMarks = checkMarks;
            _logger = logger;
        }

        public async Task<Result<AnswerDocument>> CheckAsync(RequestDocument document)
        {
            AnswerDocument answer = new();
            Dictionary<string, string> marksForCheck = document.MarkDictionary();

            if (!Constants.Online)
                return Result.Failure<AnswerDocument>("Нет интернета");

            try
            {
                await CheckMarksAsync(answer, marksForCheck);
            }
            catch (Exception ex)
            {
                var err = $"[{DateTime.Now}] - Ошибка проверки документа - {ex.Message}";
                _logger.LogWarning(err);

                return Result.Failure<AnswerDocument>(err);
            }

            return Result.Success(answer);
        }

        private async Task CheckMarksAsync(AnswerDocument answer, Dictionary<string, string> marksForCheck)
        {
            if (marksForCheck.Count == 0)
                return;

            CheckMarksRequestData checkMarksRequestData = new(marksForCheck.Values.ToList());

            var trueMarkCheckResult = await _checkMarks.RequestMarkState(checkMarksRequestData);

            trueMarkCheckResult ??= new();

            answer.Truemark_response = trueMarkCheckResult;

            if (trueMarkCheckResult.Code == 0)
            {
                foreach (var trueApiMarcData in trueMarkCheckResult.Codes)
                {
                    string markError = trueApiMarcData.MarkError();

                    trueApiMarcData.Cis = trueApiMarcData.Cis.Replace("\u001d", Convert.ToChar(29).ToString());

                    if (markError != string.Empty)
                    {
                        answer.Code = 1;
                        answer.Error = markError;
                        answer.Marking_codes.Add(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(trueApiMarcData.Cis)));
                    }
                }
            }
        }
    }
}
