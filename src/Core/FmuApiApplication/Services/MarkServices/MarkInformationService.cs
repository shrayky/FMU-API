using CouchDb.Handlers;
using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Frontol;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FrontolDb.Handlers;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.MarkServices
{
    public class MarkInformationService : IMarkService
    {
        private readonly Func<string, Task<IMark>> _markFactory;
        private readonly ILogger<MarkInformationService> _logger;
        private readonly MarksCheckService _checkMarks;
        private readonly IMarkInformationRepository _markStateService;
        private readonly IDocumentRepository _frontolDocumentService;
        private readonly FrontolSprtDataHandler? _frontolSprtDataService;
        private readonly IParametersService _parametersService;

        private readonly Parameters _configuration;

        public MarkInformationService(
            Func<string, Task<IMark>> markFactory,
            ILogger<MarkInformationService> logger,
            MarksCheckService checkMarks,
            IMarkInformationRepository markStateService,
            IDocumentRepository frontolDocumentService,
            FrontolSprtDataHandler? frontolSprtDataService,
            IParametersService parametersService)
        {
            _markFactory = markFactory;
            _logger = logger;
            _checkMarks = checkMarks;
            _markStateService = markStateService;
            _frontolDocumentService = frontolDocumentService;
            _frontolSprtDataService = frontolSprtDataService;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        
        }
        public async Task<IMark> MarkAsync(string markInBase64)
        {
            return await _markFactory(markInBase64);
        }

        public async Task<DocumentEntity> AddDocumentToDbAsync(RequestDocument data)
        {
            var operationResult = await _frontolDocumentService.Add(data);

            return operationResult.Value;
        }

        public async Task<DocumentEntity> DocumentFromDbAsync(string uid)
        {
            var operationResult = await _frontolDocumentService.Get(uid);

            return operationResult.Value;
        }

        public async Task DeleteDocumentFromDbAsync(string uid)
        {
            var operationResult = await _frontolDocumentService.Delete(uid);
        }

        public async Task<MarkEntity> MarkChangeState(string id, string newState, SaleData saleData)
        {
            bool isSold = newState == MarkState.Sold;

            var existMark = await _markStateService.GetAsync(id);

            existMark.State = newState;
            existMark.TrueApiCisData.Sold = isSold;

            MarkEntity markInformation = new()
            {
                MarkId = id,
                State = existMark.State,
                TrueApiCisData = existMark.TrueApiCisData,
                TrueApiAnswerProperties = existMark.TrueApiAnswerProperties,
                SaleData = saleData,
            };

            return await _markStateService.AddAsync(markInformation);
        }

        public async Task<MarkEntity> MarkInformationAsync(string id)
        {
            return await _markStateService.GetAsync(id);
        }

        public async Task<int> WareSaleOrganizationFromFrontolBaseAsync(string wareBarcode)
        {
            if (!_configuration.FrontolConnectionSettings.ConnectionEnable())
                return 0;

            if (wareBarcode.Length == 0)
                return 0;

            if (_frontolSprtDataService == null)
                return 0;

            var result = await _frontolSprtDataService.PrintGroupCodeByBarcodeAsync(wareBarcode);

            if (result.IsFailure)
                return 0;

            return result.Value;
        }

        public async Task<List<MarkEntity>> MarkInformationBulkAsync(List<string> sgtins)
        {
            return await _markStateService.GetDocumentsAsync(sgtins);
        }

        public async Task DraftBeerUpdateAsync(string SGtin, int Quantity)
        {
            var markData = await _markStateService.GetAsync(SGtin);

            if (markData == null)
                return;

            markData.TrueApiCisData.SoldUnitCount += Quantity;

            await _markStateService.AddAsync(markData);
        }
    }
}
