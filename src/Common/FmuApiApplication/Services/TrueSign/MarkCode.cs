using CouchDb.Handlers;
using CSharpFunctionalExtensions;
using FmuApiApplication.Utilites;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueSignApi.MarkData.Check;
using FmuApiSettings;

namespace FmuApiApplication.Services.TrueSign
{
    public class MarkCode : IMark
    {
        private readonly MarksChekerService _trueApiCheck;
        private readonly MarkInformationHandler _markStateCrud;
        public string Code { get; } = string.Empty;
        public string SGtin { get; } = string.Empty;
        public bool CodeIsSgtin { get; } = false;
        public string Barcode { get; } = string.Empty;
        public int PrintGroupCode { get; private set; } = 0;
        public string ErrorDescription { get; private set; } = string.Empty;
        private CheckMarksDataTrueApi TrueMarkData { get; set; } = new();
        private MarkInformation InformationAboutMark { get; set; } = new();
        private FmuAnswer MarkCheckAnswer {get; set;} = new();
        private char Gs { get; } = (char)29;
        private string GsE { get; } = @"\u001d";

        private MarkCode(string markCode, MarkInformationHandler markStateCrud, MarksChekerService checkMarks)
        {
            _markStateCrud = markStateCrud;
            _trueApiCheck = checkMarks;

            Code = markCode.Trim();

            // некоторые производители ошибочно пишут стартовый GS
            if (markCode.StartsWith(GsE) || markCode.StartsWith(Gs))
                Code = markCode.Substring(1);

            SGtin = CalculateSgtin(Code);
            CodeIsSgtin = (SGtin == Code);

            Barcode = SGtin.Substring(1, 13);
            Barcode = Barcode.TrimStart('0');
        }

        private string CalculateSgtin(string markCode)
        {
            string sgtin = string.Empty;

            // вся маркировка (кроме штучного табака)
            if (markCode.StartsWith("01"))
            {
                markCode = markCode.Replace(GsE, Gs.ToString());
                sgtin = markCode;

                int gsSymbolPosition = markCode.IndexOf(Gs);

                // эта ошибка из-за того, что не передается символ gs сканером
                if (gsSymbolPosition > 0)
                {
                    sgtin = $"{sgtin.Substring(2, 14)}{sgtin.Substring(18, gsSymbolPosition - 18)}";
                    return sgtin;
                }
            }

            // штучный табак
            if (markCode.Length == 29)
            {
                sgtin = markCode.Substring(0, 21);
                return sgtin;
            }

            // если нам в проверку прилетел сразу sgtin
            if (sgtin.Length == 0)
                sgtin = markCode;

            return sgtin;
        }

        public static MarkCode Create(string markingCode, MarkInformationHandler markStateCrud, MarksChekerService checkMarks)
        {
            MarkCode markCode = new(markingCode, markStateCrud, checkMarks);

            return markCode;
        }

        public static async Task<MarkCode> CreateAsync(string codeData, MarkInformationHandler markStateCrud, MarksChekerService checkMarks)
        {
            bool isMarkDecoded = StringHelper.IsDigitString(codeData.Substring(0, 14));

            if (!isMarkDecoded)
                codeData = EncodeMark(codeData);

            MarkCode markCode = new(codeData, markStateCrud, checkMarks);

            await markCode.OfflineCheckAsync();
            
            return markCode;
        }

        private static string EncodeMark(string markingCode)
        {
            string codeData = string.Empty;

            try
            {
                codeData = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(markingCode));
            }
            catch
            {
                codeData = markingCode;
            }

            return codeData;
        }

        public CheckMarksDataTrueApi TrueApiData()
        {
            return TrueMarkData ?? new();
        }

        // Метод извлекает дааные по марке из базы данных (если она подключена)
        //
        public async Task<Result<FmuAnswer>> OfflineCheckAsync()
        {
            MarkCheckAnswer = new();

            if (!Constants.Parametrs.Database.OfflineCheckIsEnabled)
                return Result.Success(MarkCheckAnswer);

            InformationAboutMark = await _markStateCrud.GetAsync(SGtin);

            if (!InformationAboutMark.HaveTrueApiAnswer)
                return Result.Success(MarkCheckAnswer);

            InformationAboutMark.TrueApiCisData.Sold = InformationAboutMark.IsSold;

            TrueMarkData = new()
            {
                Code = InformationAboutMark.TrueApiAnswerProperties.Code,
                Description = InformationAboutMark.TrueApiAnswerProperties.Description,
                ReqId = InformationAboutMark.TrueApiAnswerProperties.ReqId,
                ReqTimestamp = InformationAboutMark.TrueApiAnswerProperties.ReqTimestamp,
            };

            TrueMarkData.Codes.Add(InformationAboutMark.TrueApiCisData);

            ErrorDescription = "Данные получены в offline режиме";

            MarkCheckAnswer = new()
            {
                Code = 0,
                Error = ErrorDescription,
                Truemark_response = TrueMarkData
            };

            ResetErrorFields();

            if (TrueMarkData.AllMarksIsExpire())
                return Result.Failure<FmuAnswer>("Марка просрочена");

            if (TrueMarkData.AllMarksIsSold())
                return Result.Failure<FmuAnswer>("Марка продана");

            if (InformationAboutMark.State == MarkState.Returned & Constants.Parametrs.SaleControlConfig.BanSalesReturnedWares)
            {
                MarkCheckAnswer.Truemark_response.MarkCodeAsSaled();
                return Result.Failure<FmuAnswer>("Данные получены в offline режиме. Продажа возвращенного покупателем товара запрещена!");
            }

            return Result.Success(MarkCheckAnswer);
        }

        // Метод производит проверку марки по api честного знака
        //
        public async Task<Result> OnlineCheckAsync()
        {
            ErrorDescription = string.Empty;

            string requestCode = Code;

            var trueSignMarkData = TrueMarkData.MarkData();

            if (CodeIsSgtin && !trueSignMarkData.Empty)
                requestCode = trueSignMarkData.Cis;

            if (CodeIsSgtin && trueSignMarkData.Empty)
                Result.Failure("Онлайн проверка по неполному коду невозможна!");

            CheckMarksRequestData checkMarksRequestData = new(requestCode);

            var trueMarkCheckResult = await _trueApiCheck.RequestMarkState(checkMarksRequestData, PrintGroupCode);

            TrueMarkData = trueMarkCheckResult.Value;

            if (trueMarkCheckResult.IsFailure)
                return Result.Failure(trueMarkCheckResult.Error);

            trueSignMarkData = TrueMarkData.MarkData();

            if (trueSignMarkData.Empty)
                return Result.Failure($"Пустой результат проверки по коду марки {Code}");

            if (Constants.Parametrs.SaleControlConfig.CheckIsOwnerField && trueSignMarkData.IsOwner)
            {
                trueSignMarkData.Valid = false;
                ErrorDescription = "Нельзя продавать чужую марку!";
            }

            ResetErrorFields();

            ErrorDescription = trueSignMarkData.MarkErrorDescription();

            trueSignMarkData.Cis = trueSignMarkData.Cis.Replace(GsE, Gs.ToString());

            return Result.Success(MarkDataAfterCheck);
        }

        public void ResetErrorFields()
        {
            if (Constants.Parametrs.SaleControlConfig.IgnoreVerificationErrorForTrueApiGroups == string.Empty)
                return;

            var trueSignMarkData = TrueMarkData.MarkData();

            if (trueSignMarkData.Empty)
                return;

            if (!trueSignMarkData.InGroup(Constants.Parametrs.SaleControlConfig.IgnoreVerificationErrorForTrueApiGroups))
                return;

            trueSignMarkData.ResetErrorFileds();
        }

        public async Task<bool> Save()
        {
            if (!Constants.Parametrs.Database.OfflineCheckIsEnabled)
                return false;

            MarkInformation currentMarkState = await _markStateCrud.GetAsync(SGtin);

            foreach (var markCodeData in TrueMarkData.Codes)
            {
                string state = FmuApiDomain.MarkInformation.MarkState.Stock;

                if (currentMarkState.State != string.Empty)
                    state = currentMarkState.State;

                MarkInformation markState = new()
                {
                    MarkId = SGtin,
                    State = markCodeData.Sold ? FmuApiDomain.MarkInformation.MarkState.Sold :  state,
                    TrueApiCisData = markCodeData,
                    TrueApiAnswerProperties = new()
                    {
                        Code = TrueMarkData.Code,
                        Description = TrueMarkData.Description,
                        ReqId = TrueMarkData.ReqId,
                        ReqTimestamp = TrueMarkData.ReqTimestamp
                    }
                };

                await _markStateCrud.AddAsync(markState);

            }

            return true;
        }

        public async Task<Result> SaveAsync()
        {
            try
            {
                _ = await Save();
            }
            catch (Exception ex)
            {
                return Result.Failure(ex.Message);
            }

            return Result.Success();

        }

        public void SetPrintGroupCode(int printGroupCode)
        {
            PrintGroupCode = printGroupCode;
        }

        public MarkInformation DatabaseState()
        {
            return InformationAboutMark;
        }

        public FmuAnswer MarkDataAfterCheck()
        {
            return MarkCheckAnswer;
        }
    }
}
