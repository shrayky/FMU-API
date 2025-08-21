using CSharpFunctionalExtensions;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Frontol;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CommitDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private Lazy<ITemporaryDocumentsService> _temporaryDocumentsService { get; set; }
        private Lazy<IMarkStateManager> _markStateService { get; set; }
        private Func<string, Task<IMark>> _markFactory { get; set; }
        private ILogger<CommitDocument> _logger { get; set; }
        private IParametersService _parametersService { get; set; }
        private IApplicationState _appState { get; set; }

        private Parameters _configuration;
        const string saleDocumentType = "receipt";

        private CommitDocument(RequestDocument requestDocument, IServiceProvider provider)
        {
            _document = requestDocument;

            _temporaryDocumentsService = new Lazy<ITemporaryDocumentsService>(() => provider.GetRequiredService<ITemporaryDocumentsService>());
            _markStateService = new Lazy<IMarkStateManager>(() => provider.GetRequiredService<IMarkStateManager>());
            _markFactory = provider.GetRequiredService<Func<string, Task<IMark>>>();

            
            _logger = provider.GetRequiredService<ILogger<CommitDocument>>();
            _appState = provider.GetRequiredService<IApplicationState>();
            _parametersService = provider.GetRequiredService<IParametersService>();
            _configuration = _parametersService.Current();
        }

        private static CommitDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
            => new(requestDocument, provider);
        
        public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
            => CreateObject(requestDocument, provider);
        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            await SendDocumentToAlcoUnitAsync();

            return await CommitDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CommitDocumentAsync()
        {
            FmuAnswer checkResult = new();

            if (!_configuration.Database.ConfigurationIsEnabled)
                return Result.Success(checkResult);

            if (!_appState.CouchDbOnline())
                return Result.Success(checkResult);

            DocumentEntity frontolDocument = await _temporaryDocumentsService.Value.DocumentFromDbAsync(_document.Uid);

            if (frontolDocument.Id == string.Empty)
                return Result.Failure<FmuAnswer>($"Невозможно закрыть документ {_document.Uid}! Он не найден в базе документов!");

            SaleData saleData = new()
            {
                CheckNumber = frontolDocument.FrontolDocument.Number,
                SaleDate = DateTime.Now,
                Pos = frontolDocument.FrontolDocument.Pos,
                IsSale = frontolDocument.FrontolDocument.Type == saleDocumentType
            };

            string state = saleData.IsSale ? MarkState.Sold : MarkState.Returned;

            // Словарь: код марки (SGtin) -> количество, из фронтола марка прилетает в base64
            Dictionary<string, decimal> quantityByMark = frontolDocument.FrontolDocument.Positions
                .SelectMany(p => p.Marking_codes.Select(code => new { 
                    code = Convert.FromBase64String(code),
                    Quantity = p.Volume > 0 ? (decimal)p.Volume : (decimal)p.Quantity 
                }))
                .ToDictionary(
                    x => System.Text.Encoding.UTF8.GetString(x.code),
                    x => x.Quantity
                );

            // Получаем все объекты марки
            var marks = await Task.WhenAll(
                quantityByMark.Keys.Select(code => _markFactory(code))
            );

            // Словарь: SGtin -> информация о марке
            Dictionary<string, MarkEntity> entityBySGtin = (await _markStateService.Value.InformationBulk(
                marks.Select(m => m.SGtin).ToList()
            )).ToDictionary(e => e.MarkId);

            var draftBeerUpdates = new List<(string SGtin, decimal Quantity)>();
            var marksToChangeState = new Dictionary<string, SaleData>();

            foreach (var mark in marks)
            {
                var trueApiData = entityBySGtin[mark.SGtin].TrueApiCisData;
                var quantity = quantityByMark[mark.Code];

                saleData.Quantity = quantity;

                marksToChangeState.Add(mark.SGtin, saleData);

                if (trueApiData == null)
                    continue;
            }

            await MarkChangeStateBulk(marksToChangeState, state);
           
            await _temporaryDocumentsService.Value.DeleteDocumentFromDbAsync(_document.Uid);

            return Result.Success(checkResult);
        }

        private async Task MarkChangeStateBulk(Dictionary<string, SaleData> marksToChangeState, string state)
        {
            foreach (var mark in marksToChangeState)
            {
                await _markStateService.Value.ChangeState(mark.Key, state, mark.Value);
            }
        }

        private async Task<Result> SendDocumentToAlcoUnitAsync()
        {
            RequestDocument auDoc = _document;

            if (_configuration.FrontolAlcoUnit.NetAdres == string.Empty)
                return Result.Success(auDoc);

            return Result.Success(auDoc);
        }
    }
}
