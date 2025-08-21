using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Frontol.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckReturnDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private Lazy<IFrontolSprTService> _frontolSprTSerice { get; set; }
        private Func<string, Task<IMark>> _markFactory { get; set; }
        IParametersService _parametersService { get; set; }
        private ILogger<CheckReturnDocument> _logger { get; set; }

        private Parameters _configuration;

        private CheckReturnDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            _document = requestDocument;
            
            _frontolSprTSerice = new Lazy<IFrontolSprTService>(() => provider.GetRequiredService<IFrontolSprTService>());
            
            _logger = provider.GetRequiredService<ILogger<CheckReturnDocument>>();
                        
            _markFactory = provider.GetRequiredService<Func<string, Task<IMark>>>(); ;

            _parametersService = provider.GetRequiredService<IParametersService>();
            _configuration = _parametersService.Current();
        }

        private static CheckReturnDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
            => new(requestDocument, provider);

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
            => CreateObject(requestDocument, provider);


        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            FmuAnswer answer = new();

            // фронтол 20.5 не требовал проверки марок для документов возврата,
            // начиная с 22.4 такая проверка обязательна
            // но если в запросе есть ИНН - то это уже новая версия фронтола
            if (!_configuration.SaleControlConfig.CheckReceiptReturn && _document.Inn == "")
                return Result.Success(answer);

            var checkResult = await MarkInformation();
            checkResult.Value.FillFieldsFor6255(_document.Inn);

            if (checkResult.IsFailure)
                return checkResult;

            answer = checkResult.Value;

            // фронтол показывает ошибку, если статус марки продан, даже при возврате!
            // Приходится, вот так некрасиво, исправлять.
            if (_configuration.SaleControlConfig.ResetSoldStatusForReturn)
                answer.Truemark_response.MarkCodeAsNotSaled();

            // зачем нам анализировать поля с ошибками при возврате...
            answer.Truemark_response.ResetErrorFields(_configuration.SaleControlConfig.ResetSoldStatusForReturn);

            // фронтол зачем то проверяет срок годности при возврате, поменяем дату
            answer.Truemark_response.CorrectExpireDate();

            return Result.Success(answer);
        }

        private async Task<Result<FmuAnswer>> MarkInformation()
        {
            _logger.LogInformation("Марка для проверки {markCodeData}", _document.Mark);

            IMark mark = await _markFactory(_document.Mark);
            await SetOrganizationIdAsync(mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.ReturnSale);

            if (checkResult.IsSuccess)
                return checkResult;

            _logger.LogError(checkResult.Error);

            return checkResult;
        }

        private async Task<Result<FmuAnswer>> MarkInformation(string markInBase64)
        {
            _logger.LogInformation("Марка для проверки {markCodeData}", markInBase64);

            IMark mark = await _markFactory(markInBase64);
            await SetOrganizationIdAsync(mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.ReturnSale);

            if (checkResult.IsSuccess)
                return checkResult;

            _logger.LogError(checkResult.Error);

            return checkResult;
        }

        private async Task SetOrganizationIdAsync(IMark mark)
        {
            if (_configuration.OrganisationConfig.PrintGroups.Count == 1)
                return;

            int pgCode = 0;
            string inn = _document.Inn;

            if (inn != string.Empty)
            {
                var organisation = _configuration.OrganisationConfig.PrintGroups.FirstOrDefault(p => p.INN == inn);

                if (organisation != null)
                    pgCode = organisation.Id;
            }
            else
            {
                var result = await _frontolSprTSerice.Value.PrintGroupCodeByBarcodeAsync(mark.Barcode);

                if (result.IsSuccess)
                    pgCode = result.Value;
            }

            if (pgCode == 0)
                return;

            _logger.LogInformation("Код группы печати организации {pgCode}", pgCode);

            mark.SetPrintGroupCode(pgCode);
        }

    }
}
