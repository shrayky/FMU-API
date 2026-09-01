using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.BeerTaps.Entities;
using FmuApiDomain.BeerTaps.Interfaces;
using FmuApiDomain.BeerTaps.Models;
using FmuApiDomain.Frontol.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.BeerTaps;

[AutoRegisterService(ServiceLifetime.Scoped)]
public class BeerTapsManager : IBeerOnTapManager
{
    private readonly ILogger<BeerTapsManager> _logger;
    private readonly IBeerOnTapRepository _tapsRepository;
    private readonly IMarkParser _markParser;
    private readonly IBeerTapsRepositoryFactory _frontolBeerTapsFactory;
    private readonly IFrontolSprTServiceFactory _frontolSprTFactory;
    private readonly IParametersService _parametersService;

    public BeerTapsManager(
        ILogger<BeerTapsManager> logger,
        IBeerOnTapRepository tapsRepository,
        IMarkParser markParser,
        IBeerTapsRepositoryFactory frontolBeerTapsFactory,
        IFrontolSprTServiceFactory frontolSprTFactory,
        IParametersService parametersService)
    {
        _logger = logger;
        _tapsRepository = tapsRepository;
        _markParser = markParser;
        _frontolBeerTapsFactory = frontolBeerTapsFactory;
        _frontolSprTFactory = frontolSprTFactory;
        _parametersService = parametersService;
    }

    public async Task<Result> TapOperation(TapBeerOperation document)
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return Result.Success();

        if (!settings.SaleControlConfig.UseBeerTaps)
            return Result.Success();

        Result operationResult;
        var info = document.Position;

        if (document.Type == "connect_tap")
        {
            var mark = _markParser.EncodeMark(info.MarkingCode);
            var sgtin = _markParser.CalculateSGtin(mark);

            operationResult = await _tapsRepository.SetOnTap(sgtin, mark, info.Text, info.Id, info.Volume);
        }
        else if (document.Type == "disconnect_tap")
        {
            var mark = _markParser.EncodeMark(document.EmptiedMarkingCode);
            var sgtin = _markParser.CalculateSGtin(mark);

            operationResult = await _tapsRepository.FreeTap(sgtin);
        }
        else
            return Result.Failure($"Неизвестный тип операции постановки/снятия на кран {document.Type}");

        var syncResult = await SyncFrontolBeerTaps(settings.ConnectedFrontolSettings.ConnectionSettings);

        return operationResult.IsSuccess ? Result.Success() : Result.Failure(operationResult.Error);
    }

    public async Task<int> Volume(string sGtin)
    {
        var result = await _tapsRepository.BeerKegVolume(sGtin);

        return result.IsSuccess ? result.Value : 0;
    }

    public async Task<List<BeerOnTap>> List()
    {
        List<BeerOnTap> beerOnTaps = [];

        var listResult = await _tapsRepository.All();

        if (listResult.IsFailure)
        {
            _logger.LogError("Ошибка пролучения списка пива на кранах: {err}", listResult.Error);
            return beerOnTaps;
        }

        foreach (var item in listResult.Value)
        {
            BeerOnTap beerOnTap = new()
            {
                Id = item.Id,
                MarkCode = item.MarkCode,
                Volume = item.Volume,
                Sales = item.Sales,
                TapName = item.TapName,
                WareCode = item.WareCode,
                WareName = item.WareName,
            };

            beerOnTaps.Add(beerOnTap);
        }

        return beerOnTaps;
    }

    public async Task<Result> AddSale(string sGtin, int saledVolume)
        => await _tapsRepository.AddSale(sGtin, saledVolume);

    public async Task<Result> SyncFrontolBeerTaps(List<FrontolConnectionSettings> frontolConnections)
    {
        var localBeerTapsResult = await _tapsRepository.All();

        if (localBeerTapsResult.IsFailure)
            return Result.Failure(localBeerTapsResult.Error);

        var localBeerTaps = localBeerTapsResult.Value;

        var localTapsByMark = localBeerTaps
            .Where(x => !string.IsNullOrWhiteSpace(x.MarkCode))
            .ToDictionary(x => x.MarkCode, StringComparer.OrdinalIgnoreCase);

        foreach (var connection in frontolConnections)
        {
            if (!connection.ConnectionEnable())
                continue;

            var repo = _frontolBeerTapsFactory.Create(connection.ConnectionStringBuild());

            var frontolMarks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var frontolBeerTapsResult = await repo.All();

                if (frontolBeerTapsResult.IsFailure)
                {
                    _logger.LogWarning("Синхронизация кранов: ошибка чтения базы Frontol {Name} ({Path}): {Error}",
                        connection.Name,
                        connection.Path,
                        frontolBeerTapsResult.Error);

                    continue;
                }

                var frontolBeerTaps = frontolBeerTapsResult.Value;

                foreach (var frontolTap in frontolBeerTaps)
                {
                    // это "пустой" кран
                    if (frontolTap.MarkCode == string.Empty)
                        continue;

                    var mark = _markParser.EncodeMark(frontolTap.MarkCode);
                    var sgtin = _markParser.CalculateSGtin(mark);

                    frontolMarks.Add(frontolTap.MarkCode);

                    BeerTapEntity? localBeerTap;

                    localTapsByMark.TryGetValue(mark, out localBeerTap);

                    if (localBeerTap == null)
                    {
                        // нет в локальной базе на кране этой марки, нужно снять во фронтоле
                        var operationResult = await repo.FreeTapByMark(frontolTap.MarkCode);

                        if (operationResult.IsFailure)
                        {
                            _logger.LogWarning("Синхронизация кранов: ошибка снятия с крана марки {MarkCode} базы Frontol {Name} ({Path}): {Error}",
                                frontolTap.MarkCode,
                                connection.Name,
                                connection.Path,
                                operationResult.Error);

                            continue;
                        }

                        continue;
                    }

                    if (localBeerTap.TapName == string.Empty)
                    {
                        // нет в локальной базе информации о кране на котором стоит марка
                        var operationResult = await _tapsRepository.LinkMarkToTap(sgtin, frontolTap.TapName);

                        if (operationResult.IsFailure)
                        {
                            _logger.LogWarning("Синхронизация кранов: ошибка привзяки крана {tpaName} к марке {MarkCode} базы Frontol {Name} ({Path}): {Error}",
                                frontolTap.TapName,
                                frontolTap.MarkCode,
                                connection.Name,
                                connection.Path,
                                operationResult.Error);

                            continue;
                        }

                        continue;
                    }
                }

                // проверим марки которые есть локально, но их нет во фронтоле
                foreach (var localBeerTap in localBeerTaps)
                {
                    if (string.IsNullOrWhiteSpace(localBeerTap.MarkCode))
                        continue;

                    if (frontolMarks.Contains(localBeerTap.MarkCode))
                        continue;

                    if (localBeerTap.TapName == string.Empty)
                        continue;

                    var wareCodeParseSucces = int.TryParse(localBeerTap.WareCode, out int wareCode);

                    if (!wareCodeParseSucces)
                    {
                        _logger.LogWarning("Синхронизация кранов: ошибка постановки на кран {tpaName} в базе фронтола марки {MarkCode} базы Frontol {Name} ({Path}): {Error}",
                            localBeerTap.TapName,
                            localBeerTap.MarkCode,
                            connection.Name,
                            connection.Path,
                            "ошибка преобразования кода товара в int");

                        continue;
                    }

                    BeerTap frontolBeerTap = new()
                    {
                        MarkCode = localBeerTap.MarkCode,
                        TapName = localBeerTap.TapName,
                        Volume = localBeerTap.Volume,
                        WareCode = wareCode
                    };

                    var operationResult = await repo.SetOnTap(frontolBeerTap);

                    if (operationResult.IsFailure)
                    {
                        _logger.LogWarning("Синхронизация кранов: ошибка постановки на кран {tapName} в базе фронтола марки {MarkCode} базы Frontol {Name} ({Path}): {Error}",
                            localBeerTap.TapName,
                            localBeerTap.MarkCode,
                            connection.Name,
                            connection.Path,
                            operationResult.Error);

                        continue;
                    }

                }

            }
            finally
            {
                repo.Dispose();
            }
        }

        return Result.Success();
    }

    public async Task<Result<int>> LoadFromFrontol(int connectionId)
    {
        var settings = await _parametersService.CurrentAsync();

        if (!settings.Database.Enable)
            return Result.Failure<int>("CouchDB выключена");

        if (connectionId < 1)
            return Result.Failure<int>("Не выбрана база Frontol для загрузки кранов");

        var connection = settings.ConnectedFrontolSettings.ConnectionSettings
            .FirstOrDefault(p => p.Id == connectionId);

        if (connection == null || !connection.ConnectionEnable())
            return Result.Failure<int>("Выбранное подключение Frontol недоступно");

        using var tapsRepo = _frontolBeerTapsFactory.Create(connection.ConnectionStringBuild());
        using var sprtRepo = _frontolSprTFactory.Create(connection.ConnectionStringBuild());

        var frontolBeerTapsResult = await tapsRepo.All();

        if (frontolBeerTapsResult.IsFailure)
            return Result.Failure<int>(frontolBeerTapsResult.Error);

        var placedKegs = frontolBeerTapsResult.Value
            .Where(tap => !string.IsNullOrWhiteSpace(tap.MarkCode))
            .ToList();

        if (placedKegs.Count == 0)
            return Result.Success(0);

        var wareIds = placedKegs
            .Where(tap => tap.WareId > 0)
            .Select(tap => tap.WareId)
            .Distinct()
            .ToList();

        var waresResult = await sprtRepo.GetWaresByIdsAsync(wareIds);

        if (waresResult.IsFailure)
            return Result.Failure<int>(waresResult.Error);

        var wares = waresResult.Value;
        var loaded = 0;

        foreach (var frontolTap in placedKegs)
        {
            var mark = _markParser.EncodeMark(frontolTap.MarkCode);
            var sgtin = _markParser.CalculateSGtin(mark);

            wares.TryGetValue(frontolTap.WareId, out var ware);

            if (frontolTap.WareId > 0 && ware == null)
            {
                _logger.LogWarning(
                    "Первичная загрузка кранов: товар WareId {WareId} не найден в SPRT базы Frontol {Name}",
                    frontolTap.WareId,
                    connection.Name);
            }

            var wareName = ware?.Name ?? string.Empty;
            var wareCode = ware != null ? ware.Code.ToString() : frontolTap.WareCode.ToString();
            var volume = Convert.ToInt32(Math.Round(frontolTap.Volume));

            var setResult = await _tapsRepository.SetOnTap(
                sgtin,
                mark,
                wareName,
                wareCode,
                volume,
                frontolTap.TapName);

            if (setResult.IsFailure)
            {
                _logger.LogWarning(
                    "Первичная загрузка кранов: ошибка записи кега {MarkCode} крана {TapName}: {Error}",
                    frontolTap.MarkCode,
                    frontolTap.TapName,
                    setResult.Error);

                continue;
            }

            loaded++;
        }

        _logger.LogInformation(
            "Первичная загрузка кранов из Frontol {Name}: загружено {Loaded} из {Total}",
            connection.Name,
            loaded,
            placedKegs.Count);

        return Result.Success(loaded);
    }
}
