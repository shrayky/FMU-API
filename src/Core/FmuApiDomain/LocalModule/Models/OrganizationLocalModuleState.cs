using FmuApiDomain.LocalModule.Enums;

namespace FmuApiDomain.LocalModule.Models
{
    public class OrganizationLocalModuleState
    {
        public int Organization { get; set; } = 0;
        public LocalModuleStatus Status { get; set; } = LocalModuleStatus.Unknown;
    }
}
