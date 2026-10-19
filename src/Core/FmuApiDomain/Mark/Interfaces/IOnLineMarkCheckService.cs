using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.TrueApi.MarkData.Check;

namespace FmuApiDomain.Mark.Interfaces;

public interface IOnLineMarkCheckService
{
    Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, int organizationCode);
    Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks, PrintGroupData group);
    Task<Result<CheckMarksDataTrueApi>> RequestMarkState(CheckMarksRequestData marks);
}
