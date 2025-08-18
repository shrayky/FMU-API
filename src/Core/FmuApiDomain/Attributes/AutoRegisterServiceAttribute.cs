using Microsoft.Extensions.DependencyInjection;

namespace FmuApiDomain.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class AutoRegisterServiceAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; }

        public AutoRegisterServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            Lifetime = lifetime;
        }
    }
}