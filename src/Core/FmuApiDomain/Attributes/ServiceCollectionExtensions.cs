using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FmuApiDomain.Attributes
{
    public static class ServiceCollectionExtensions
    {
        public static void AddAutoRegisteredServices(this IServiceCollection services, Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes()
                    .Where(t => t.GetCustomAttribute<AutoRegisterServiceAttribute>() != null && t.IsClass && !t.IsAbstract);

                foreach (var type in types)
                {
                    var attr = type.GetCustomAttribute<AutoRegisterServiceAttribute>()!;
                    var interfaces = type.GetInterfaces();

                    if (interfaces.Length > 0)
                    {
                        services.Add(new ServiceDescriptor(interfaces[0], type, attr.Lifetime));
                    }
                    else
                    {
                        services.Add(new ServiceDescriptor(type, type, attr.Lifetime));
                    }
                }
            }
        }
    }
}