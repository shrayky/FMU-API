using CSharpFunctionalExtensions;
using FmuApiDomain.TsPiot.Models;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.TsPiot.Interfaces;

public interface ITsPiotService
{
    Task<Result<CheckMarksDataTrueApi>> Check(string mark, TsPiotConnectionSettings connectionSettings);
}