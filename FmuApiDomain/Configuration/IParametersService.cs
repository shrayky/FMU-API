namespace FmuApiDomain.Configuration
{
    public interface IParametersService
    {
        Task<Parameters> CurrentAsync();
        Parameters Current();
        Task UpdateAsync(Parameters parameters);
    }
}
