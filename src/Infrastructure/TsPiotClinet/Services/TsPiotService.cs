using System.Net.Http.Json;
using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.TrueApi.MarkData.Check;
using FmuApiDomain.TsPiot.Interfaces;
using FmuApiDomain.TsPiot.Models;
using Microsoft.Extensions.Logging;
using Shared.Json;
using TsPiotClinet.Models;

namespace TsPiotClinet.Services;

public class TsPiotService : ITsPiotService
{
    private readonly ILogger<TsPiotService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    
    private const string CheckAddress = @"/api/v1/codes/check";

    public TsPiotService(ILogger<TsPiotService> logger, IHttpClientFactory httpClientFactory)
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

            var status = await JsonHelpers.DeserializeAsync<TsPiotMarkCheckResponse>(content);

            if (status == null)
                return Result.Failure<CheckMarksDataTrueApi>("Пустой ответ от сервера");

            if (status.Code != 0)
            {
                return Result.Failure<CheckMarksDataTrueApi>(
                    $"Ошибка работы с ТСПИоТ код {status.Code}: {status.Message}");
            }

            var result = new CheckMarksDataTrueApi
            {
                Code = 0,
                Description = string.Empty,
                ReqId = status.Response.RequestId,
                ReqTimestamp = status.Response.RequestTimestamp,
                Inst = status.Response.LocalModuleInstance,
                Version = status.Response.LocalModuleVersion,
                Codes = status.Response.Codes
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