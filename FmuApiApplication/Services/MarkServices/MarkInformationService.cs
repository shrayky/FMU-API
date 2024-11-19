using FmuApiApplication.Services.Frontol;
using FmuApiApplication.Services.TrueSign;
using FmuApiCouhDb.CrudServices;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiSettings;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.MarkStateServices
{
    public class MarkInformationService : IMarkInformationService
    {
        private readonly ILogger<MarkInformationService> _logger;
        private readonly MarksChekerService _checkMarks;
        private readonly MarkInformationHandler _markStateService;
        private readonly FrontolDocumentHandler _frontolDocumentService;
        private readonly FrontolSprtDataHandler? _frontolSprtDataService;

        public MarkInformationService(ILogger<MarkInformationService> logger, MarksChekerService checkMarks, MarkInformationHandler markStateService, FrontolDocumentHandler frontolDocumentService, FrontolSprtDataHandler? frontolSprtDataService)
        {
            _logger = logger;
            _checkMarks = checkMarks;
            _markStateService = markStateService;
            _frontolDocumentService = frontolDocumentService;
            _frontolSprtDataService = frontolSprtDataService;
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

        public async Task<IMark> MarkAsync(string markInbase64)
        {
            return await MarkCode.CreateAsync(markInbase64, _markStateService, _checkMarks);
        }

        public async Task<MarkInformation> MarkChangeState(string id, string newState, SaleData saleData)
        {
            bool isSold = (newState == MarkState.Sold);

            var exsitMark = await _markStateService.GetAsync(id);

            exsitMark.State = newState;
            exsitMark.TrueApiCisData.Sold = isSold;

            MarkInformation markInformation = new()
            {
                MarkId = id,
                State = exsitMark.State,
                TrueApiCisData = exsitMark.TrueApiCisData,
                TrueApiAnswerProperties = exsitMark.TrueApiAnswerProperties,
                SaleData = saleData,
            };

            return await _markStateService.AddAsync(markInformation);
        }

        public async Task<MarkInformation> MarkInformationAsync(string id)
        {
            return await _markStateService.GetAsync(id);
        }

        public async Task<int> WareSaleOrganisationFromFrontolBaseAsync(string wareBarcode)
        {
            if (!Constants.Parametrs.FrontolConnectionSettings.ConnectionEnable())
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
