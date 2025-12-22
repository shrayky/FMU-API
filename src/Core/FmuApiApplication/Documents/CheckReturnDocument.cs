using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckReturnDocument : IFrontolDocumentService
    {
        private RequestDocument Document { get; set; }
        private IMarkFabric MarkFabric { get; set; }
        private IParametersService ParametersService { get; set; }
        private ILogger<CheckReturnDocument> Logger { get; set; }

        private readonly Parameters _configuration;

        private CheckReturnDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            Document = requestDocument;
            
            Logger = provider.GetRequiredService<ILogger<CheckReturnDocument>>();
                        
            MarkFabric = provider.GetRequiredService<IMarkFabric>();

            ParametersService = provider.GetRequiredService<IParametersService>();
            _configuration = ParametersService.Current();
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
            if (!_configuration.SaleControlConfig.CheckReceiptReturn && Document.Inn == "")
                return Result.Success(answer);

            var checkResult = await MarkInformation();
            checkResult.Value.FillFieldsFor6255(Document.Inn);

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

            return Result.Success(answer);
        }

        private async Task<Result<FmuAnswer>> MarkInformation()
        {
            Logger.LogInformation("Марка для проверки {markCodeData}", Document.Mark);

            var mark = await MarkFabric.Create(Document.Positions[0], Document.Mark);

            var checkResult = await mark.PerformCheckAsync(OperationType.ReturnSale);

            if (checkResult.IsSuccess)
                return checkResult;

            Logger.LogError(checkResult.Error);

            return checkResult;
        }
    }
}
