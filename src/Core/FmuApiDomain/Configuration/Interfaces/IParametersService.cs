using CSharpFunctionalExtensions;
using FmuApiDomain.DTO.FmuApiExchangeData;

namespace FmuApiDomain.Configuration.Interfaces;

public interface IParametersService
{
    Task<Parameters> CurrentAsync();
    Parameters Current();
    Task UpdateAsync(Parameters parameters);
    Task<Result> ApplyFromCentral(FmuApiSetting value);
}
