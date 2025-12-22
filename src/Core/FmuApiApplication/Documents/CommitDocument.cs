using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FmuApiApplication.Documents
{
    public class CommitDocument : IFrontolDocumentService
    {
        private RequestDocument Document { get; set; }
        private Lazy<IDocumentRepository> TemporaryDocumentsService { get; set; }
        private Lazy<IMarkStateManager> MarkStateService { get; set; }
        private IMarkFabric MarkFabric { get; set; }
        private IParametersService ParametersService { get; set; }
        private IApplicationState AppState { get; set; }

        private readonly Parameters _configuration;
        private const string SaleDocumentType = "receipt";

        private CommitDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            Document = requestDocument;

            TemporaryDocumentsService = new Lazy<IDocumentRepository>(provider.GetRequiredService<IDocumentRepository>);
            MarkStateService = new Lazy<IMarkStateManager>(provider.GetRequiredService<IMarkStateManager>);
            MarkFabric = provider.GetRequiredService<IMarkFabric>();

            AppState = provider.GetRequiredService<IApplicationState>();
            ParametersService = provider.GetRequiredService<IParametersService>();
            _configuration = ParametersService.Current();
        }

        private static CommitDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
            => new(requestDocument, provider);

        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
            => CreateObject(requestDocument, provider);

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            var sendResult = await SendDocumentToAlcoUnit();

            if (sendResult.IsFailure)
                return Result.Failure<FmuAnswer>(sendResult.Error);

            return await CommitDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CommitDocumentAsync()
        {
            FmuAnswer checkResult = new();

            if (!_configuration.Database.ConfigurationIsEnabled)
                return Result.Success(checkResult);

            if (!AppState.CouchDbOnline())
                return Result.Success(checkResult);

            var loadDocumentResult = await TemporaryDocumentsService.Value.Get(Document.Uid);

            if (loadDocumentResult.IsFailure)
                return Result.Failure<FmuAnswer>(
                    $"Невозможно закрыть документ {Document.Uid}! Он не найден в базе документов!");

            var frontolDocument = loadDocumentResult.Value;

            SaleData saleData = new()
            {
                CheckNumber = frontolDocument.FrontolDocument.Number,
                SaleDate = DateTime.Now,
                Pos = frontolDocument.FrontolDocument.Pos,
                IsSale = frontolDocument.FrontolDocument.Type == SaleDocumentType
            };

            var state = saleData.IsSale ? MarkState.Sold : MarkState.Returned;

            // Словарь: код марки (SGtin) -> количество, из фронтола марка прилетает в base64
            Dictionary<string, decimal> quantityByMark = frontolDocument.FrontolDocument.Positions
                .SelectMany(p => p.Marking_codes.Select(code => new
                {
                    code = Convert.FromBase64String(code),
                    Quantity = p.Volume > 0 ? (decimal)p.Volume : (decimal)p.Quantity
                }))
                .ToDictionary(
                    x => System.Text.Encoding.UTF8.GetString(x.code),
                    x => x.Quantity
                );

            // Получаем все объекты марки
            var marks = await Task.WhenAll(
                quantityByMark.Keys.Select(code => MarkFabric.Create(new(), code))
            );

            var marksToChangeState = new Dictionary<string, SaleData>();

            foreach (var mark in marks)
            {
                var quantity = quantityByMark[mark.Code];
                saleData.Quantity = quantity;
                marksToChangeState.Add(mark.SGtin, saleData);
            }

            await MarkChangeStateBulk(marksToChangeState, state);

            await TemporaryDocumentsService.Value.Delete(Document.Uid);

            return Result.Success(checkResult);
        }

        private async Task MarkChangeStateBulk(Dictionary<string, SaleData> marksToChangeState, string state)
        {
            foreach (var mark in marksToChangeState)
            {
                await MarkStateService.Value.ChangeState(mark.Key, state, mark.Value);
            }
        }

        private async Task<Result> SendDocumentToAlcoUnit()
        {
            if (string.IsNullOrEmpty(_configuration.FrontolAlcoUnit.NetAdres))
                return Result.Success();

            await Task.Delay(1);

            var auDoc = Document;
            
            return Result.Success(auDoc);
        }
    }
}
