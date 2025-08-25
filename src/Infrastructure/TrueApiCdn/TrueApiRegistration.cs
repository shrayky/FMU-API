using FmuApiDomain.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TrueApiCdn.Interface;
using TrueApiCdn.Services;
using TrueApiCdn.Workers;

namespace TrueApiCdn
{
    public class TrueApiRegistration
    {
        public static void AddService(IServiceCollection services)
        {
            services.AddAutoRegisteredServices([Assembly.GetExecutingAssembly()]);

            services.AddHostedService<CdnLoaderWorker>();
        }
    }
}
