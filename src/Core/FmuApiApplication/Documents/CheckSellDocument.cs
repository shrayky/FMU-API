using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Database.Dto;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.Fmu.PacketTrapper.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.Repositories;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents;

public class CheckSellDocument : IFrontolDocumentService
{
    private ILogger<CheckSellDocument> _logger { get; set; }
    private IMarkFabric _markFabric { get; set; }
    IParametersService _parametersService { get; set; }
    private ICheckStatisticRepository _checkStatisticRepository { get; set; }
    private IFmuPacketTrapper _packetTrapper { get; set; }

    private RequestDocument _document { get; set; }
    private const int CacheExpirationMinutes = 30;
    private readonly Parameters _configuration;

    private CheckSellDocument(RequestDocument requestDocument, IServiceProvider provider)
    {
        _document = requestDocument;

        _markFabric = provider.GetRequiredService<IMarkFabric>();

        _checkStatisticRepository = provider.GetRequiredService<ICheckStatisticRepository>();

        _logger = provider.GetRequiredService<ILogger<CheckSellDocument>>();
        _parametersService = provider.GetRequiredService<IParametersService>();
        _configuration = _parametersService.Current();
        _packetTrapper = provider.GetRequiredService<IFmuPacketTrapper>();
    }

    private static CheckSellDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
        => new(requestDocument, provider);

    public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
        => CreateObject(requestDocument, provider);

    public async Task<Result<FmuAnswer>> ActionAsync()
    {
        var checkResult = await MarkInformation();

        if (checkResult.IsSuccess)
            await _packetTrapper.SaveCheckResultForCashRegister(_document, checkResult.Value);

        return checkResult;
    }

    private async Task<Result<FmuAnswer>> MarkInformation()
    {
        _logger.LogInformation("Марка для проверки {markCodeData}", _document.Mark);
        
        var mark = await _markFabric.Create(_document.Positions[0], _document.Mark);

        var checkResult = await mark.PerformCheckAsync(OperationType.Sale);
        
        if (checkResult.IsSuccess)
        {
            var markInformation = checkResult.Value;

            markInformation.FillFieldsForFrontol_6_25_5(_document.Inn);
            markInformation.FillFieldsForIMark(_document.RequestFromAppId);

            await SaveCheckStatistic(mark.SGtin, markInformation, failed: false);

            return Result.Success(markInformation);
        }
        else
        {
            await SaveCheckStatistic(mark.SGtin, answer: null, failed: true);
        }

        _logger.LogError(checkResult.Error);

        return checkResult;
    }

    private async Task SaveCheckStatistic(string sgtin, FmuAnswer? answer, bool failed)
    {
        var online = !failed && answer is { OfflineRegime: false };
        var offline = !failed && answer is { OfflineRegime: true };
        var success = !failed && string.IsNullOrEmpty(answer?.Error);

        var entity = new StatisticEntity
        {
            SGtin = sgtin,
            CheckDate = DateTime.Now,
            SuccessCheck = success,
            OnLineCheck = online,
            OffLineCheck = offline,
            WarningMessage = failed ? string.Empty : (answer?.Error ?? string.Empty),
            CheckRequest = _document,
            CheckResponse = answer
        };

        await _checkStatisticRepository.Add(entity);
    }

    private FmuAnswer CreateFakeAnswer(IMark mark, string error)
    {
        var fakeCodeData = new CodeDataTrueApi
        {
            Cis = mark.Code,
            PrintView = mark.SGtin,
            Gtin = mark.Barcode,
            Valid = true,
            Verified = true,
            Found = true,
            Utilised = true,
            IsOwner = true,
            IsBlocked = false,
            IsTracking = false,
            Sold = false,
            Realizable = true,
            GrayZone = false,
        };

        var truemarkResponse = new CheckMarksDataTrueApi
        {
            Code = 0,
            Description = $"Ошибка проверки маркировки. {error}",
            ReqId = "00000000-0000-0000-0000-000000000000",
            ReqTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
            Codes = [fakeCodeData]
        };

        _logger.LogWarning("[{Date}] - Ошибка проверки кода марки {Code}: {Error}",
            DateTime.Now, mark.Code, error);

        return new FmuAnswer
        {
            Code = 1,
            Error = error,
            Truemark_response = truemarkResponse
        };
    }
}
