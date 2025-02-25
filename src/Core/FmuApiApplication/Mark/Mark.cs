using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Mark.Models;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.Logging;
using Shared.Strings;

namespace FmuApiApplication.Mark
{
    public class Mark : IMark
    {
        private readonly IMarkParser _markParser;
        private readonly IMarkChecker _markChecker;
        private readonly IMarkStateManager _markStateManager;
        private readonly IParametersService _parametersService;
        private readonly ILogger<Mark> _logger;
        private readonly Parameters _configuration;

        public string Code { get; }
        public string SGtin { get; }
        public string Cis { get; }
        public bool CodeIsSgtin { get; }
        public string Barcode { get; }
        public int PrintGroupCode { get; private set; }
        public string ErrorDescription { get; private set; } = string.Empty;

        private MarkCheckResult _lastCheckResult = MarkCheckResult.Empty();
        private delegate Task<MarkCheckResult> CheckDelegate();

        private Mark(
            string markCode,
            IMarkParser markParser,
            IMarkChecker markChecker,
            IMarkStateManager markStateManager,
            IParametersService parametersService,
            ILogger<Mark> logger)
        {
            _markParser = markParser;
            _markChecker = markChecker;
            _markStateManager = markStateManager;
            _parametersService = parametersService;
            _logger = logger;
            _configuration = _parametersService.Current();

            Code = _markParser.ParseCode(markCode);
            SGtin = _markParser.CalculateSGtin(Code);
            Cis = _markParser.CalculateCis(Code);
            CodeIsSgtin = (SGtin == Code);
            Barcode = _markParser.CalculateBarcode(SGtin);

            _logger.LogInformation(
                "Создан объект марки: Code={Code}, SGtin={SGtin}, CodeIsSgtin={CodeIsSgtin}, Barcode={Barcode}",
                Code, SGtin, CodeIsSgtin, Barcode);
        }

        public static Mark Create(
            string codeData,
            IMarkParser markParser,
            IMarkChecker markChecker,
            IMarkStateManager markStateManager,
            IParametersService parametersService,
            ILogger<Mark> logger)
        {
            bool isMarkDecoded = StringHelpers.IsDigitString(codeData.Substring(0, 14));
            if (!isMarkDecoded)
            {
                codeData = markParser.EncodeMark(codeData);
            }

            var markCode = new Mark(
                codeData,
                markParser,
                markChecker,
                markStateManager,
                parametersService,
                logger);

            return markCode;
        }

        public async Task<Result<FmuAnswer>> PerformCheckAsync(OperationType operation)
        {
            _logger.LogInformation("Начало проверки марки {Code}", Code);

            var delegates = new CheckDelegate[]
            {
                async () => await _markChecker.OnlineCheck(Code, SGtin, CodeIsSgtin, PrintGroupCode),
                async () => await _markChecker.OfflineCheckAsync(Cis, PrintGroupCode),
                async() => await _markChecker.FmuApiDatabaseCheck(SGtin, _markStateManager)
            };

            List<string> checkErrors = [];
            var currentState = await _markStateManager.GetMarkInformation(SGtin);

            foreach (var check in delegates)
            {
                _lastCheckResult = await check();

                if (!_lastCheckResult.IsSuccess)
                {
                    checkErrors.Add(_lastCheckResult.ErrorDescription);
                    continue;
                }
                
                if (!_lastCheckResult.HasTrueApiAnswer())
                    continue;

                if (currentState.State == MarkState.Sold)
                    _lastCheckResult.TrueMarkData.Codes.ForEach(code => code.Sold = true);

                if (_lastCheckResult.MarkInformation.State != currentState.State)
                    _lastCheckResult.MarkInformation.State = currentState.State;

                var validationResult = ValidateMarkData(operation);
                
                if (validationResult.IsFailure)
                {
                    _lastCheckResult.UpdateErrorDescription(validationResult.Error);
                    ErrorDescription = _lastCheckResult.FmuAnswer.Error;
                    _lastCheckResult.SetUnsuccess();

                    _logger.LogWarning("При валидации данных для марки {Code} обнаружены ошибки: {Error}",
                                       Code,
                                       validationResult.Error);
                }

                if (!_lastCheckResult.FmuAnswer.Offline && validationResult.IsSuccess)
                    await _markStateManager.SaveMarkInformation(SGtin, _lastCheckResult.TrueMarkData);

                checkErrors.Clear();

                break;
            }

            if (checkErrors.Count == 0)
                return Result.Success(_lastCheckResult.FmuAnswer);
                
            return Result.Failure<FmuAnswer>($"Проверка марки {Code} не удалась по причине: {string.Join(", ", checkErrors)}");

        }

        public Result ValidateMarkData(OperationType operation)
        {
            var trueMarkData = _lastCheckResult.TrueMarkData;
            var markData = trueMarkData.MarkData();
            var markDbInfo = _lastCheckResult.MarkInformation;
            var markError = string.Empty;

            if (operation == OperationType.ReturnSale)
            {
                if (!trueMarkData.AllMarksIsSold() && markDbInfo.State == MarkState.Sold)
                    return Result.Failure("Вернуть можно только проданные марки");
            }
            else
            {
                // Проверка владельца
                if (_configuration.SaleControlConfig.CheckIsOwnerField && !markData.IsOwner)
                {
                    markData.Valid = false;
                    return Result.Failure("Нельзя продавать чужую марку!");
                }

                // Проверка срока годности
                if (trueMarkData.AllMarksIsExpire())
                {
                    return Result.Failure("Срок годности истек");
                }

                // Проверка продажи
                if (trueMarkData.AllMarksIsSold() || markDbInfo.State == MarkState.Sold)
                {
                    return Result.Failure("Марка продана");
                }

                // Проверка продажи возвращенного товара
                if (_lastCheckResult.MarkInformation.State == MarkState.Returned &&
                    _configuration.SaleControlConfig.BanSalesReturnedWares)
                {
                    _lastCheckResult.FmuAnswer.Truemark_response.MarkCodeAsSaled();

                    return Result.Failure("Продажа возвращенного покупателем товара запрещена!");
                }

                // Сброс ошибок верификации, для указанных в настройках групп
                ResetErrorFields();

                markError = markData.MarkErrorDescription();
            }

            if (!string.IsNullOrEmpty(markError))
            {
                return Result.Failure(markError);
            }

            return Result.Success();
        }

        public void SetPrintGroupCode(int printGroupCode)
        {
            PrintGroupCode = printGroupCode;
            _logger.LogInformation("Установлен код группы печати {PrintGroupCode} для марки {Code}",
                PrintGroupCode, Code);
        }

        public CheckMarksDataTrueApi TrueApiData()
        {
            return _lastCheckResult.TrueMarkData;
        }

        public FmuApiDomain.MarkInformation.Entities.MarkEntity DatabaseState()
        {
            return _lastCheckResult.MarkInformation;
        }

        public FmuAnswer MarkDataAfterCheck()
        {
            return _lastCheckResult.FmuAnswer;
        }

        private void ResetErrorFields()
        {
            if (string.IsNullOrEmpty(_configuration.SaleControlConfig.IgnoreVerificationErrorForTrueApiGroups))
                return;

            var markData = _lastCheckResult.TrueMarkData.MarkData();
            if (!markData.Empty && markData.InGroup(_configuration.SaleControlConfig.IgnoreVerificationErrorForTrueApiGroups))
            {
                markData.ResetErrorFileds();
            }
        }

    }
}
