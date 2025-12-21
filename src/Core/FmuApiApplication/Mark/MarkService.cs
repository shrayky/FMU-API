using FmuApiApplication.Mark.Interfaces;
using FmuApiApplication.Mark.Services;
using FmuApiDomain.Configuration.Interfaces;
using FmuApiDomain.MarkInformation.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FmuApiApplication.Mark
{
    public static class MarkService
    {
        public static IServiceCollection AddMarkServices(this IServiceCollection services)
        {
            // Регистрация базовых сервисов
            services.AddScoped<IMarkParser, MarkParser>();
            services.AddScoped<IMarkChecker, MarkChecker>();
            services.AddScoped<IMarkStateManager, MarkStateManager>();
            services.AddScoped<ICheckOperation, TsPiotClient>();

            // Фабрика для создания IMark
            services.AddScoped<Func<string, Task<IMark>>>(sp => async mark =>
            {
                var markParser = sp.GetRequiredService<IMarkParser>();
                var markChecker = sp.GetRequiredService<IMarkChecker>();
                var markStateManager = sp.GetRequiredService<IMarkStateManager>();
                var parametersService = sp.GetRequiredService<IParametersService>();
                var tsPiotClient = sp.GetRequiredService<ICheckOperation>();
                var logger = sp.GetRequiredService<ILogger<Mark>>();

                return Mark.Create(
                    mark,
                    markParser,
                    markChecker,
                    markStateManager,
                    parametersService,
                    tsPiotClient,
                    logger);
            });

            return services;
        }
    }
}
