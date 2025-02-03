using Microsoft.Extensions.DependencyInjection;
using TrueApiCdn.Interface;
using TrueApiCdn.Services;
using TrueApiCdn.Workers;

namespace TrueApiCdn
{
    public class TrueApiCdnRegistration
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddSingleton<ICdnService, SimpleCdnService>();
            services.AddHostedService<CdnLoaderWorker>();
        }
    }
}
