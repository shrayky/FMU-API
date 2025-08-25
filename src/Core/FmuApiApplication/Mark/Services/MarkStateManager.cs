// Ignore Spelling: Fmu Gtin

using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Entities;
using FmuApiDomain.MarkInformation.Enums;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.MarkInformation.Models;
using FmuApiDomain.Repositories;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Mark.Services
{
    public class MarkStateManager : IMarkStateManager
    {
        private readonly ILogger<MarkStateManager> _logger;
        private readonly IMarkInformationRepository _markCrud;
        private readonly IParametersService _parametersService;
        private readonly IApplicationState _appState;

        private readonly Parameters _configuration;

        public MarkStateManager(IServiceProvider services)
        {
            _markCrud = services.GetRequiredService<IMarkInformationRepository>();
            _parametersService = services.GetRequiredService<IParametersService>();
            _logger = services.GetRequiredService<ILogger<MarkStateManager>>();
            _appState = services.GetRequiredService<IApplicationState>();
            _configuration = _parametersService.Current();
        }

        public async Task<MarkEntity> Information(string sGtin)
        {
            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                _logger.LogInformation("БД отключена, информация о {sgtin} не получена.", sGtin);
                return new MarkEntity();
            }

            if (!_appState.CouchDbOnline())
            {
                _logger.LogWarning("БД не в сети, информация о {sgtin} не получена", sGtin);
                return new MarkEntity();
            }

            var markInfo = await _markCrud.GetAsync(sGtin);

            if (markInfo.HaveTrueApiAnswer)
                _logger.LogInformation("Получена информация о марке {Sgtin}", sGtin);
            else
                _logger.LogError("Ошибка при получении информации о марке {Sgtin}", sGtin);
            
            return new MarkEntity();
        }

        public async Task<List<MarkEntity>> InformationBulk(List<string> sGtins)
        {
            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                _logger.LogInformation("БД отключена, информация о {sgtins} не получена.", sGtins);
                return [];
            }

            if (!_appState.CouchDbOnline())
            {
                _logger.LogWarning("БД не в сети, информация о {sgtins} не получена", sGtins);
                return [];
            }
            
            return await _markCrud.GetDocumentsAsync(sGtins);
        }

        public async Task<Result> Save(string sGtin, CheckMarksDataTrueApi trueMarkData)
        {
            if (!_configuration.Database.DatabaseCheckIsEnabled)
            {
                _logger.LogInformation("БД отключена, сохранение в базу данных {sgtin} пропущено.", sGtin);
                return Result.Success();
            }

            if (!_appState.CouchDbOnline())
            {
                _logger.LogWarning("БД не в сети, сохранение в базу данных {sgtin} пропущено.", sGtin);
                return Result.Success();
            }

            var currentMarkState = await _markCrud.GetAsync(sGtin);

            TrueApiAnswerData trueApiAnswerData = new()
            {
                Code = trueMarkData.Code,
                Description = trueMarkData.Description,
                ReqId = trueMarkData.ReqId,
                ReqTimestamp = trueMarkData.ReqTimestamp,
                Inst = trueMarkData.Inst,
                Version = trueMarkData.Version
            };

            foreach (var markCodeData in trueMarkData.Codes)
            {
                var mark = MarkEntity.Create(sGtin, markCodeData, currentMarkState, trueApiAnswerData);
                
                var savedMark = await _markCrud.AddAsync(mark);

                if (savedMark.Id == string.Empty)
                    return Result.Failure("");
            }

            _logger.LogInformation("Информация о марке {Sgtin} сохранена", sGtin);

            return Result.Success();
        }

        public async Task<Result> UpdateMarkState(string sgtin, string newState)
        {
            if (!_configuration.Database.DatabaseCheckIsEnabled)
                return Result.Success();

            if (!_appState.CouchDbOnline())
            {
                _logger.LogWarning("БД не в сети, сохранение в базу данных {sgtin} пропущено.", sgtin);
                return Result.Success();
            }

            var markInfo = await _markCrud.GetAsync(sgtin);

            if (markInfo.Id == string.Empty)
                return Result.Failure("");

            markInfo.State = newState;
            await _markCrud.AddAsync(markInfo);

            _logger.LogInformation("Состояние марки {Sgtin} обновлено на {State}", sgtin, newState);
            return Result.Success();
        }

        public async Task<MarkEntity> ChangeState(string sGtin, string newState, SaleData saleData)
        {
            bool isSold = newState == MarkState.Sold;
            var existMark = await _markCrud.GetAsync(sGtin);
            var markInfo = existMark.TrueApiCisData;

            if (markInfo.InnerUnitCount != null)
            {
                var reaminWithSale = markInfo.InnerUnitCount - (markInfo.SoldUnitCount ?? 0) - saleData.Quantity;

                newState = reaminWithSale > 0 ? MarkState.Stock : MarkState.Sold;
                isSold = newState == MarkState.Sold;

                markInfo.SoldUnitCount += (int)saleData.Quantity;
            }

            existMark.State = newState;
            existMark.TrueApiCisData.Sold = isSold;

            var markEntity = MarkEntity.Create(sGtin, markInfo, existMark, existMark.TrueApiAnswerProperties);

            markEntity.Sales.Add(saleData);

            return await _markCrud.AddAsync(markEntity);
        }

        public async Task<bool> MarkIsSold(string sgtin)
        {
            if (!_configuration.Database.DatabaseCheckIsEnabled)
                return false;

            if (!_appState.CouchDbOnline())
                return false;

            var markInfo = await _markCrud.GetAsync(sgtin);
            return markInfo.State == MarkState.Sold;
        }
    }
}
