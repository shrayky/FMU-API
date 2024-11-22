using FmuApiDomain.Configuration;
using FmuApiSettings;
using Interfaces;
using Microsoft.Extensions.Logging;

namespace ApllicationConfigurationService
{
    public class SimpleParametersService : IParametersService
    {
        private readonly ILogger<SimpleParametersService> _logger;

        public SimpleParametersService(ILogger<SimpleParametersService> logger)
        {
            _logger = logger;
        }

        public Parametrs Current()
        {
            return Constants.Parametrs;
        }

        public async Task<Parametrs> CurrentAsync()
        {
            return await Task.Run(() => { return Current(); });
        }

        public async Task UpdateAsync(Parametrs parametrs)
        {
            await Constants.Parametrs.SaveAsync(parametrs, Constants.DataFolderPath);
        }
    }
}



