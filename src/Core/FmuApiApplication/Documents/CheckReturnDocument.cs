using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Fmu.PacketTrapper.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckReturnDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkFabric _markFabric { get; set; }
        private IParametersService _parametersService { get; set; }
        private ILogger<CheckReturnDocument> _logger { get; set; }
        private IFmuPacketTrapper _packetTrapper { get; set; }

        private readonly Parameters _configuration;

        private CheckReturnDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            _document = requestDocument;

            _logger = provider.GetRequiredService<ILogger<CheckReturnDocument>>();
            _markFabric = provider.GetRequiredService<IMarkFabric>();
            _parametersService = provider.GetRequiredService<IParametersService>();
            _packetTrapper = provider.GetRequiredService<IFmuPacketTrapper>();

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
            // начиная с 22.4 такая проверка обязательна,
            // но если в запросе есть ИНН - то это уже новая версия фронтола
            if (!_configuration.SaleControlConfig.CheckReceiptReturn && _document.Inn == "")
                return Result.Success(answer);

            var checkResult = await MarkInformation();
            checkResult.Value.FillFieldsForFrontol_6_25_5(_document.Inn);

            if (checkResult.IsFailure)
                return checkResult;

            answer = checkResult.Value;

            // фронтол показывает ошибку, если статус марки продан, даже при возврате!
            // Приходится, вот так некрасиво, исправлять.
            if (_configuration.SaleControlConfig.ResetSoldStatusForReturn)
                answer.Truemark_response.MarkCodeAsNotSaled();

            // зачем нам анализировать поля с ошибками при возврате...
            answer.Truemark_response.ResetErrorFields(_configuration.SaleControlConfig.ResetSoldStatusForReturn);

            // фронтол зачем-то проверяет срок годности при возврате, поменяем дату
            answer.Truemark_response.CorrectExpireDate();

            await _packetTrapper.SaveCheckResultForCashRegister(_document, checkResult.Value);

            return Result.Success(answer);
        }

        private async Task<Result<FmuAnswer>> MarkInformation()
        {
            _logger.LogInformation("Марка для проверки {markCodeData}", _document.Mark);

            var mark = await _markFabric.Create(_document.Positions[0], _document.Mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.ReturnSale);

            if (checkResult.IsSuccess)
                return checkResult;

            _logger.LogError(checkResult.Error);

            return checkResult;
        }
    }
}
