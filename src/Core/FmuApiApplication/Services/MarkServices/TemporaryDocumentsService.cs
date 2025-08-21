using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Frontol;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.Repositories;
using FrontolDb.Handlers;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Services.MarkServices
{
    public class TemporaryDocumentsService : ITemporaryDocumentsService
    {
        private readonly ILogger<TemporaryDocumentsService> _logger;
        private readonly IDocumentRepository _frontolDocumentService;
        private readonly FrontolSprtDataHandler? _frontolSprtDataService;
        private readonly IParametersService _parametersService;

        private readonly Func<string, Task<IMark>> _markFactory;

        private readonly Parameters _configuration;

        public TemporaryDocumentsService(
            Func<string, Task<IMark>> markFactory,
            ILogger<TemporaryDocumentsService> logger,
            IDocumentRepository frontolDocumentService,
            FrontolSprtDataHandler? frontolSprtDataService,
            IParametersService parametersService)
        {
            _markFactory = markFactory;
            _logger = logger;
            _frontolDocumentService = frontolDocumentService;
            _frontolSprtDataService = frontolSprtDataService;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();
        
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
            await _frontolDocumentService.Delete(uid);
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
