using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.Fmu.Document.Enums;
using FmuApiDomain.Fmu.Document.Interface;
using FmuApiDomain.MarkInformation.Interfaces;
using FmuApiDomain.Repositories;
using FmuApiDomain.TrueApi.MarkData;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Documents;

public class CheckSellDocument : IFrontolDocumentService
{
    private RequestDocument Document { get; set; }
    private ILogger<CheckSellDocument> Logger { get; set; }
    private IMarkFabric MarkFabric { get; set; }
    private readonly IMemoryCache _memcachedClient;
    IParametersService ParametersService { get; set; }
    private ICheckStatisticRepository CheckStatisticRepository { get; set; }

    private const int CacheExpirationMinutes = 30;
    private readonly Parameters _configuration;

    private CheckSellDocument(RequestDocument requestDocument, IServiceProvider provider)
    {
        Document = requestDocument;

        MarkFabric = provider.GetRequiredService<IMarkFabric>();

        CheckStatisticRepository = provider.GetRequiredService<ICheckStatisticRepository>();

        Logger = provider.GetRequiredService<ILogger<CheckSellDocument>>();
        _memcachedClient = provider.GetRequiredService<IMemoryCache>();
        ParametersService = provider.GetRequiredService<IParametersService>();
        _configuration = ParametersService.Current();
    }

    private static CheckSellDocument CreateObject(RequestDocument requestDocument, IServiceProvider provider)
        => new(requestDocument, provider);

    public static IFrontolDocumentService Create(RequestDocument requestDocument, IServiceProvider provider)
        => CreateObject(requestDocument, provider);

    public async Task<Result<FmuAnswer>> ActionAsync()
    {
        var checkResult = await MarkInformation();
                    
        return checkResult;
    }

    private async Task<Result<FmuAnswer>> MarkInformation()
    {
        Logger.LogInformation("Марка для проверки {markCodeData}", Document.Mark);
        
        var mark = await MarkFabric.Create(Document.Positions[0], Document.Mark);

        var checkResult = await mark.PerformCheckAsync(OperationType.Sale);
        
        if (checkResult.IsSuccess)
        {
            var markInformation = checkResult.Value;

            markInformation.FillFieldsForFrontol_6_25_5(Document.Inn);

            if (markInformation.Error == string.Empty && !markInformation.OfflineRegime)
                await CheckStatisticRepository.SuccessOnLineCheck(markInformation.SGtin(), DateTime.Now);
            else if (markInformation.Error == string.Empty && markInformation.OfflineRegime)
                await CheckStatisticRepository.SuccessOffLineCheck(markInformation.SGtin(), DateTime.Now);
            else if (markInformation.Error != string.Empty && !markInformation.OfflineRegime)
                await CheckStatisticRepository.OnLineCheckWithWarnings(markInformation.SGtin(), DateTime.Now, markInformation.Error);
            else if (markInformation.Error != string.Empty && markInformation.OfflineRegime)
                await CheckStatisticRepository.OffLineCheckWithWarnings(markInformation.SGtin(), DateTime.Now, markInformation.Error);

            return Result.Success(markInformation);
        }
        else
        {
            await CheckStatisticRepository.FailureCheck(mark.SGtin, DateTime.Now);
        }

        Logger.LogError(checkResult.Error);

        if (_configuration.SaleControlConfig.SendEmptyTrueApiAnswerWhenTimeoutError
            && _configuration.SaleControlConfig.RejectSalesWithoutCheckInformationFrom < DateTime.Now)
            return Result.Success(CreateFakeAnswer(mark, checkResult.Error));
        else
            return checkResult;
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

        Logger.LogWarning("[{Date}] - Ошибка проверки кода марки {Code}: {Error}",
            DateTime.Now, mark.Code, error);

        return new FmuAnswer
        {
            Code = 1,
            Error = error,
            Truemark_response = truemarkResponse
        };
    }
}
