using FmuApiDomain.Configuration;

namespace Interfaces
{
    public interface IParametersService
    {
        Task<Parametrs> CurrentAsync();

        Parametrs Current();
        Task UpdateAsync(Parametrs parametrs);
    }
}
