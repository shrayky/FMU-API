using CouchDb.Handlers;
using FmuApiApplication.Services.TrueSign;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FrontolDb.Handlers;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.MarkServices
{
    public class MarkInformationService : IMarkService
    {
        private readonly Func<string, Task<IMark>> _markFactory;
        private readonly ILogger<MarkInformationService> _logger;
        private readonly MarksCheckService _checkMarks;
        private readonly MarkInformationHandler _markStateService;
        private readonly FrontolDocumentHandler _frontolDocumentService;
        private readonly FrontolSprtDataHandler? _frontolSprtDataService;
        private readonly IParametersService _parametersService;

        private readonly Parameters _configuration;

        public MarkInformationService(
            Func<string, Task<IMark>> markFactory,
            ILogger<MarkInformationService> logger,
            MarksCheckService checkMarks,
            MarkInformationHandler markStateService,
            FrontolDocumentHandler frontolDocumentService,
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

        public async Task<IFrontolDocumentData> AddDocumentToDbAsync(IFrontolDocumentData data)
        {
            return await _frontolDocumentService.AddAsync(data);
        }

        public async Task<IFrontolDocumentData> DocumentFromDbAsync(string uid)
        {
            return await _frontolDocumentService.GetAsync(uid);
        }

        public async Task DeleteDocumentFromDbAsync(string uid)
        {
            await _frontolDocumentService.DelteAsync(uid);
        }

        public async Task<FmuApiDomain.MarkInformation.Entities.MarkEntity> MarkChangeState(string id, string newState, SaleData saleData)
        {
            bool isSold = newState == MarkState.Sold;

            var existMark = await _markStateService.GetAsync(id);

            existMark.State = newState;
            existMark.TrueApiCisData.Sold = isSold;

            FmuApiDomain.MarkInformation.Entities.MarkEntity markInformation = new()
            {
                MarkId = id,
                State = existMark.State,
                TrueApiCisData = existMark.TrueApiCisData,
                TrueApiAnswerProperties = existMark.TrueApiAnswerProperties,
                SaleData = saleData,
            };

            return await _markStateService.AddAsync(markInformation);
        }

        public async Task<FmuApiDomain.MarkInformation.Entities.MarkEntity> MarkInformationAsync(string id)
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
    }
}
