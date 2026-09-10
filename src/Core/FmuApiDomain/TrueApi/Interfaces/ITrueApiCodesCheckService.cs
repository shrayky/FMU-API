using CSharpFunctionalExtensions;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.TrueApi.Interfaces;

/// <summary>
/// Проверка кодов маркировки по контракту True API со всеми доступными источниками.
/// </summary>
public interface ITrueApiCodesCheckService
{
    Task<Result<CheckMarksDataTrueApi>> Check(CheckMarksRequestData request, string? xApiKey);
}
