using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiApplication.Mark.Models
{
    public class MarkCheckResult
    {
        public bool IsSuccess { get; private set; }
        public string ErrorDescription { get; private set; } = string.Empty;
        public CheckMarksDataTrueApi TrueMarkData { get; private set; } = new();
        public MarkEntity MarkInformation { get; private set; } = new();
        public FmuAnswer FmuAnswer { get; private set; } = new();

        private MarkCheckResult() { }

        public static MarkCheckResult Empty()
        {
            return new MarkCheckResult();
        }

        public static MarkCheckResult Success(
            CheckMarksDataTrueApi trueMarkData,
            MarkEntity markInfo,
            FmuAnswer fmuAnswer)
        {
            return new MarkCheckResult
            {
                IsSuccess = true,
                TrueMarkData = trueMarkData,
                MarkInformation = markInfo,
                FmuAnswer = fmuAnswer
            };
        }

        public static MarkCheckResult Failure(string error)
        {
            return new MarkCheckResult
            {
                IsSuccess = false,
                ErrorDescription = error
            };
        }
        public static MarkCheckResult FromCheck(Result<CheckMarksDataTrueApi> onlineCheckResult)
        {
            if (onlineCheckResult.IsFailure)
            {
                return Failure(onlineCheckResult.Error);
            }

            var fmuAnswer = new FmuAnswer
            {
                Code = 0,
                Error = string.Empty,
                Truemark_response = onlineCheckResult.Value,
                Offline = false,
                OfflineRegime = false
            };

            return new MarkCheckResult
            {
                IsSuccess = true,
                TrueMarkData = onlineCheckResult.Value,
                FmuAnswer = fmuAnswer
            };
        }

        public void UpdateErrorDescription(string error)
        {
            ErrorDescription = error;
            if (!string.IsNullOrEmpty(error))
            {
                FmuAnswer.Code = 1;
                FmuAnswer.Error = error;
            }
        }

        public void SetUnsuccess()
        {
            IsSuccess = false;
        }

        public void SetMarkInformation(MarkEntity markInfo)
        {
            MarkInformation = markInfo;
        }

        public void MarkAsNotSold()
        {
            FmuAnswer.Truemark_response?.MarkCodeAsNotSaled();
        }

        public bool IsSold()
        {
            return MarkInformation.State == MarkState.Sold;
        }

        public bool HasTrueApiAnswer()
        {
            return MarkInformation.HaveTrueApiAnswer;
        }
    }
}