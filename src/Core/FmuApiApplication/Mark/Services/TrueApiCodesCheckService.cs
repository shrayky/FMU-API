using CSharpFunctionalExtensions;
using FmuApiApplication.Mark.Interfaces;
using FmuApiDomain.Attributes;
using FmuApiDomain.Documents.Enums;
using FmuApiDomain.TrueApi.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Mark.Services;

/// <summary>
/// Проверяет коды маркировки цепочкой источников и отдаёт ответ в формате True API.
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public class TrueApiCodesCheckService(
    IMarkFabric markFabric,
    ILogger<TrueApiCodesCheckService> logger) : ITrueApiCodesCheckService
{
    public async Task<Result<CheckMarksDataTrueApi>> Check(CheckMarksRequestData request, string? xApiKey)
    {
        var codes = request.Codes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Replace("\\u001d", "\u001d"))
            .ToList();

        if (codes.Count == 0)
            return Result.Failure<CheckMarksDataTrueApi>("Список codes обязателен");

        var aggregated = new CheckMarksDataTrueApi();
        List<string> errors = [];

        foreach (var code in codes)
        {
            try
            {
                var mark = await markFabric.CreateFromCode(code, xApiKey);
                var checkResult = await mark.PerformCheckAsync(OperationType.Sale);
                var trueApiData = await mark.TrueApiData();

                if (trueApiData.Codes.Count == 0)
                {
                    errors.Add(checkResult.IsFailure ? checkResult.Error : $"Пустой результат проверки по коду {code}");
                    continue;
                }

                Merge(aggregated, trueApiData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка проверки кода {Code} через codes/check", code);
                errors.Add($"Ошибка проверки кода {code}: {ex.Message}");
            }
        }

        if (aggregated.Codes.Count == 0)
        {
            var error = errors.Count == 0
                ? "Проверка кодов не удалась"
                : string.Join(", ", errors);

            return Result.Failure<CheckMarksDataTrueApi>(error);
        }

        return Result.Success(aggregated);
    }

    private static void Merge(CheckMarksDataTrueApi target, CheckMarksDataTrueApi source)
    {
        if (target.Codes.Count == 0)
        {
            target.Code = source.Code;
            target.Description = source.Description;
            target.ReqId = source.ReqId;
            target.ReqTimestamp = source.ReqTimestamp;
            target.Inst = source.Inst;
            target.Version = source.Version;
        }

        target.Codes.AddRange(source.Codes);
    }
}
