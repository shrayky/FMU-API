﻿using CSharpFunctionalExtensions;
using FmuApiApplication.Services.MarkServices;
using FmuApiDomain.Cache.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents
{
    public class CommitDocument : IFrontolDocumentService
    {
        private RequestDocument _document { get; set; }
        private IMarkService _markInformationService { get; set; }
        private ICacheService _cacheService { get; set; }
        private ILogger _logger { get; set; }
        private IParametersService _parametersService { get; set; }

        private Parameters _configuration;
        const string saleDocumentType = "receipt";

        private CommitDocument(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            _document = requestDocument;
            _markInformationService = markInformationService;
            _cacheService = cacheService;
            _parametersService = parametersService;
            _logger = logger;

            _configuration = parametersService.Current();
        }

        private static CommitDocument CreateObject(
            RequestDocument requestDocument,
            IMarkService markInformationService,
            ICacheService cacheService,
            IParametersService parametersService,
            IApplicationState applicationStateService,
            ILogger logger)
        {
            return new CommitDocument(requestDocument, markInformationService, cacheService, parametersService, applicationStateService, logger);
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
            await SendDocumentToAlcoUnitAsync();

            return await CommitDocumentAsync();
        }

        private async Task<Result<FmuAnswer>> CommitDocumentAsync_old()
        {
            FmuAnswer checkResult = new();

            if (_configuration.Database.FrontolDocumentsDbName == string.Empty)
                return Result.Success(checkResult);

            IFrontolDocumentData frontolDocument = await _markInformationService.DocumentFromDbAsync(_document.Uid);

            if (frontolDocument.Id == string.Empty)
                return Result.Failure<FmuAnswer>($"Невозможно закрыть документ {_document.Uid}! Он не найден в базе документов!");

            SaleData saleData = new()
            {
                CheckNumber = frontolDocument.Document.Number,
                SaleDate = DateTime.Now,
                Pos = frontolDocument.Document.Pos,
                IsSale = frontolDocument.Document.Type == saleDocumentType
            };

            string state = saleData.IsSale ? MarkState.Sold : MarkState.Returned;

            foreach (var position in frontolDocument.Document.Positions)
            {
                foreach (string markInBase64 in position.Marking_codes)
                {
                    var mark = await _markInformationService.MarkAsync(markInBase64);
                    var markEntity = await _markInformationService.MarkInformationAsync(mark.SGtin);

                    var trueApiData = markEntity.TrueApiCisData;

                    if (trueApiData.InGroup(TrueApiGroup.Beer.ToString()) && trueApiData.InnerUnitCount != null)
                    {
                        if (trueApiData.InnerUnitCount - (trueApiData.SoldUnitCount ?? 0) - position.Quantity * 1000 > 0)
                            continue;
                    }

                    //mark.TrueApiData = markEntity;

                    await _markInformationService.MarkChangeState(mark.SGtin, state, saleData);
                }
            }

            await _markInformationService.DeleteDocumentFromDbAsync(_document.Uid);

            return Result.Success(checkResult);
        }

        private async Task<Result<FmuAnswer>> CommitDocumentAsync()
        {
            FmuAnswer checkResult = new();

            if (_configuration.Database.FrontolDocumentsDbName == string.Empty)
                return Result.Success(checkResult);

            IFrontolDocumentData frontolDocument = await _markInformationService.DocumentFromDbAsync(_document.Uid);

            if (frontolDocument.Id == string.Empty)
                return Result.Failure<FmuAnswer>($"Невозможно закрыть документ {_document.Uid}! Он не найден в базе документов!");

            SaleData saleData = new()
            {
                CheckNumber = frontolDocument.Document.Number,
                SaleDate = DateTime.Now,
                Pos = frontolDocument.Document.Pos,
                IsSale = frontolDocument.Document.Type == saleDocumentType
            };

            string state = saleData.IsSale ? MarkState.Sold : MarkState.Returned;

            // Словарь: код марки (SGtin) -> количество, из фронтола марка прилетает в base64
            Dictionary<string, decimal> quantityByMark = frontolDocument.Document.Positions
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
                quantityByMark.Keys.Select(code => _markInformationService.MarkAsync(code))
            );

            // Словарь: SGtin -> информация о марке
            Dictionary<string, MarkEntity> entityBySGtin = (await _markInformationService.MarkInformationBulkAsync(
                marks.Select(m => m.SGtin).ToList()
            )).ToDictionary(e => e.MarkId);

            var draftBeerUpdates = new List<(string SGtin, decimal Quantity)>();
            var marksToChangeState = new List<string>();

            foreach (var mark in marks)
            {
                var trueApiData = entityBySGtin[mark.SGtin].TrueApiCisData;
                var quantity = quantityByMark[mark.SGtin];

                marksToChangeState.Add(mark.SGtin);

                if (trueApiData == null)
                    continue;

                if (trueApiData.InGroup(TrueApiGroup.Beer.ToString()) && 
                    trueApiData.InnerUnitCount != null &&
                    trueApiData.InnerUnitCount - (trueApiData.SoldUnitCount ?? 0) - quantity * 1000> 0)
                {
                    draftBeerUpdates.Add((mark.SGtin, quantity * 1000));
                }
            }
            
            await MarkChangeStateBulk(marksToChangeState, state, saleData);
            await DraftBeerUpdateBulk(draftBeerUpdates);
            await _markInformationService.DeleteDocumentFromDbAsync(_document.Uid);

            return Result.Success(checkResult);
        }

        private async Task MarkChangeStateBulk(List<string> marksToChangeState, string state, SaleData saleData)
        {
            foreach (var mark in marksToChangeState)
            {
                await _markInformationService.MarkChangeState(mark, state, saleData);
            }
        }

        private async Task DraftBeerUpdateBulk(List<(string SGtin, decimal Quantity)> draftBeerUpdates)
        {
            foreach (var (SGtin, Quantity) in draftBeerUpdates)
            {
                await _markInformationService.DraftBeerUpdateAsync(SGtin, (int)Quantity);
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
