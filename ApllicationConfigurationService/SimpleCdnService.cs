using FmuApiDomain.Configuration.Options.TrueSign;
using FmuApiSettings;
using Interfaces;
using Microsoft.Extensions.Logging;

namespace ApllicationConfigurationService
{
    public class SimpleCdnService : ICdnService
    {
        private readonly ILogger<SimpleCdnService> _logger;

        public SimpleCdnService(ILogger<SimpleCdnService> logger)
        {
            _logger = logger;
        }
        public async Task<CdnData> CurrentAsync()
        {
            return await Task.Run(() => {return Constants.Cdn; });
        }
    }
}
