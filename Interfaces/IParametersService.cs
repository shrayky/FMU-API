using FmuApiDomain.Configuration;

namespace Interfaces
{
    public interface IParametersService
    {
        Task<Parameters> CurrentAsync();
        Parameters Current();
        Task UpdateAsync(Parameters parameters);
    }
}
