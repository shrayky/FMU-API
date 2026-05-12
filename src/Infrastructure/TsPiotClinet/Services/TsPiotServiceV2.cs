using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.TrueApi.MarkData.Check;
using FmuApiDomain.TsPiot.Interfaces;
using FmuApiDomain.TsPiot.Models;
using Microsoft.Extensions.Logging;
using Shared.Json;
using TsPiotClinet.Models;

namespace TsPiotClinet.Services;

public class TsPiotServiceV2 : ITsPiotService
{
    private readonly ILogger<TsPiotServiceV2> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    
    private const string CheckAddress = @"/api/v2/codes/check";

    public TsPiotServiceV2(ILogger<TsPiotServiceV2> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<CheckMarksDataTrueApi>> Check(string mark, TsPiotConnectionSettings connectionSettings)
    {        
        using var httpClient = _httpClientFactory.CreateClient("TsPiot");

        httpClient.BaseAddress = new Uri($"{connectionSettings.Host}:{connectionSettings.Port}");

        var markBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mark));
        
        var requestData = new TsPiotCheckMarkRequest()
        {
            ClientInfo = new(),
            Codes = [markBase64]
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync(CheckAddress, requestData);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Ошибка в ответе от TsPiot: {StatusCode}, {Error}", response.StatusCode,
                    errorContent);
                return Result.Failure<CheckMarksDataTrueApi>($"Ошибка HTTP: {response.StatusCode} {errorContent}");
            }

            var content = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("Ответ ТСПИоТ: {content}", content);
            
            var tsPiotOptions = new JsonSerializerOptions(JsonSerializeOptionsProvider.Default())
            {
                Converters = 
                { 
                    new JsonStringOrLongConverter(),
                    new JsonDateTimeStringConverter()
                }
            };

            var status = await JsonHelpers.DeserializeAsync<TsPiotOnlineCheckResponseV2>(content, tsPiotOptions);

            if (status == null)
                return Result.Failure<CheckMarksDataTrueApi>("Пустой ответ от сервера");

            if (status.CodesResponseItems.Count == 0)
            {
                return Result.Failure<CheckMarksDataTrueApi>("Пустой ответ от сервера: отсутствуют элементы codesResponse");
            }

            var firstItem = status.CodesResponseItems[0];

            if (firstItem.Code != 0)
            {
                return Result.Failure<CheckMarksDataTrueApi>(
                    $"Ошибка проверки марки через ТСПИоТ код {firstItem.Code}: {firstItem.Description}");
            }

            var result = new CheckMarksDataTrueApi
            {
                Code = firstItem.Code,
                Description = firstItem.Description,
                ReqId = firstItem.RequestId,
                ReqTimestamp = firstItem.RequestTimestamp,
                Inst = firstItem.LocalModuleInstance,
                Version = firstItem.LocalModuleVersion,
                Codes = firstItem.Codes
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("Ошибка запроса в ТСПиОТ: {Exception}", ex);
            return Result.Failure<CheckMarksDataTrueApi>(ex.Message);
        }
    }
}