using FmuApiCouhDb.CrudServices;
using FmuApiDomain.Models.Fmu.Document;
using FmuApiDomain.Models.MarkInformation;
using FmuApiDomain.Models.TrueSignApi.MarkData.Check;
using FmuApiSettings;

namespace FmuApiApplication.Services.TrueSign
{
    public class MarkCode
    {
        private readonly CheckMarks _trueApiCheck;
        private readonly MarkInformationHandler _markStateCrud;
        public string Code { get; } = string.Empty;
        public string SGtin { get; } = string.Empty;
        public bool CodeIsSgtin { get; } = false;
        public string Barcode { get; } = string.Empty;
        public int PrintGroupCode { get; private set; } = 0;
        public string ErrorDescription { get; private set; } = string.Empty;
        private CheckMarksDataTrueApi TrueMarkData { get; set; } = new();
        private MarkInformation State { get; set; } = new();
        private char Gs { get; } = (char)29;
        private string GsE { get; } = @"\u001d";

        private MarkCode(string markCode, MarkInformationHandler markStateCrud, CheckMarks checkMarks)
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

        public static MarkCode Create(string markingCode, MarkInformationHandler markStateCrud, CheckMarks checkMarks)
        {
            MarkCode markCode = new(markingCode, markStateCrud, checkMarks);

            return markCode;
        }

        public static async Task<MarkCode> CreateAsync(string encodedMarkingCode, MarkInformationHandler markStateCrud, CheckMarks checkMarks)
        {
            var decodedMarCode = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedMarkingCode));

            MarkCode markCode = new(decodedMarCode, markStateCrud, checkMarks);

            await markCode.OfflineCheckAsync();
            
            return markCode;
        }

        public CheckMarksDataTrueApi TrueApiData()
        {
            return TrueMarkData ?? new();
        }

        // Метод извлекает дааные по марке из базы данных (если она подключена)
        //
        // Возвращаемое значение:
        //  Булево - true - если с маркой все в порядке,
        //         - false - если в результате оффлайн проверки с маркой есть проблеммы
        //           (истек срок годности или она уже продана)
        public async Task<(bool result, FmuAnswer answer)> OfflineCheckAsync()
        {
            FmuAnswer defaultAnswer = new();

            if (!Constants.Parametrs.Database.OfflineCheckIsEnabled)
                return (true, defaultAnswer);

            State = await _markStateCrud.GetAsync(SGtin);

            if (!State.HaveTrueApiAnswer)
                return (true, defaultAnswer);

            State.TrueApiCisData.Sold = State.IsSold;

            TrueMarkData = new()
            {
                Code = State.TrueApiAnswerProperties.Code,
                Description = State.TrueApiAnswerProperties.Description,
                ReqId = State.TrueApiAnswerProperties.ReqId,
                ReqTimestamp = State.TrueApiAnswerProperties.ReqTimestamp,
            };

            TrueMarkData.Codes.Add(State.TrueApiCisData);

            ErrorDescription = "Данные получены в offline режиме";
            
            FmuAnswer answer = new()
            {
                Code = 0,
                Error = ErrorDescription,
                Truemark_response = TrueMarkData
            };

            ResetErrorFields();

            if (TrueMarkData.AllMarksIsExpire() || TrueMarkData.AllMarksIsSold())
                return (false, answer);

            if (State.State == MarkState.Returned & Constants.Parametrs.SaleControlConfig.BanSalesReturnedWares)
            {
                ErrorDescription = "Данные получены в offline режиме. Продажа возвращенного покупателем товара запрещена!";
                answer.Truemark_response.MarkCodeAsSaled();
                return (false, answer);
            }

            return (true, answer);
        }

        // Метод производит проверку марки по api честного знака
        //
        // Возвращаемое значение:
        //  Булево - true - все хорошо
        //
        public async Task<bool> OnlineCheckAsync()
        {
            ErrorDescription = string.Empty;

            string requestCode = Code;

            var trueSignMarkData = TrueMarkData.MarkData();

            if (CodeIsSgtin && !trueSignMarkData.Empty)
                requestCode = trueSignMarkData.Cis;

            if (CodeIsSgtin && trueSignMarkData.Empty)
            {
                ErrorDescription = "Онлайн проверка по неполному коду невозможна!";
                return false;
            }

            CheckMarksRequestData checkMarksRequestData = new(requestCode);

            var trueMarkCheckResult = await _trueApiCheck.RequestMarkState(checkMarksRequestData, PrintGroupCode);

            TrueMarkData = trueMarkCheckResult.Value;

            if (trueMarkCheckResult.IsFailure)
            {
                ErrorDescription = trueMarkCheckResult.Error;
                return false;
            }

            trueSignMarkData = TrueMarkData.MarkData();

            if (trueSignMarkData.Empty)
            {
                ErrorDescription = $"Пустой результат проверки по коду марки {Code}";
                return false;
            }
            else 
            {
                ResetErrorFields();

                ErrorDescription = trueSignMarkData.MarkErrorDescription();

                trueSignMarkData.Cis = trueSignMarkData.Cis.Replace(GsE, Gs.ToString());
            }

            return ErrorDescription == string.Empty;
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

            trueSignMarkData.Found = true;
            trueSignMarkData.Verified = true;
            trueSignMarkData.Realizable = true;
            trueSignMarkData.Utilised = true;
            trueSignMarkData.Sold = false;
            trueSignMarkData.ErrorCode = 0;
        }

        public async Task<bool> Save()
        {
            if (!Constants.Parametrs.Database.OfflineCheckIsEnabled)
                return false;

            MarkInformation currentMarkState = await _markStateCrud.GetAsync(SGtin);

            foreach (var markCodeData in TrueMarkData.Codes)
            {
                string state = MarkState.Stock;

                if (currentMarkState.State != string.Empty)
                    state = currentMarkState.State;

                MarkInformation markState = new()
                {
                    MarkId = SGtin,
                    State = markCodeData.Sold ? MarkState.Sold :  state,
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

        public void SetPrintGroup(int printGroupCode)
        {
            PrintGroupCode = printGroupCode;
        }

        public MarkInformation DatabaseState()
        {
            return State;
        }
    }
}
