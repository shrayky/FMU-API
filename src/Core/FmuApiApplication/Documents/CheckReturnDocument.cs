﻿using CSharpFunctionalExtensions;
using FmuApiApplication.Services.MarkServices;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.State.Interfaces;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CheckReturnDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkService _markInformationService { get; set; }
        private IFrontolDocumentService CheckService { get; set; }
        private ICacheService CacheService { get; set; }
        IParametersService _parametersService { get; set; }
        private ILogger _logger { get; set; }

        private Parameters _configuration;

        private CheckReturnDocument(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            CacheService = cacheService;
            _logger = logger;
            _parametersService = parametersService;

            _configuration = _parametersService.Current();

            CheckService = CheckSellDocument.Create(_document, _markInformationService, cacheService, _parametersService, applicationStateService, _logger);
        }

        private static CheckReturnDocument CreateObject(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return new CheckReturnDocument(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public static IFrontolDocumentService Create(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return CreateObject(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
        }

        public async Task<Result<FmuAnswer>> ActionAsync()
        {
            FmuAnswer answer = new();

            // фронтол 20.5 не требовал проверки марок для документов возврата,
            // начиная с 22.4 такая проверка обязательна
            if (!_configuration.SaleControlConfig.CheckReceiptReturn)
                return Result.Success(answer);

            var checkResult = await MarkInformation(_document.Mark());

            if (checkResult.IsFailure)
                return checkResult;

            answer = checkResult.Value;

            // фронтол показывает ошибку, если статус марки продан, даже при возврате!
            // Приходится, вот так некрасиво, исправлять.
            answer.Truemark_response.MarkCodeAsNotSaled();

            // зачем нам анализировать поля с ошибками при возврате...
            answer.Truemark_response.ResetErrorFields();

            // фронтол зачем то проверяет срок годности при возврате, поменяем дату
            answer.Truemark_response.CorrectExpireDate();

            return Result.Success(answer);
        }

        private async Task<Result<FmuAnswer>> MarkInformation(string markInBase64)
        {
            _logger.LogInformation("Марка для проверки {markCodeData}", markInBase64);

            IMark mark = await _markInformationService.MarkAsync(markInBase64);
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

            int pgCode = await _markInformationService.WareSaleOrganizationFromFrontolBaseAsync(mark.Barcode);

            if (pgCode == 0)
                return;

            _logger.LogInformation("Код группы печати организации {pgCode}", pgCode);

            mark.SetPrintGroupCode(pgCode);
        }

    }
}
