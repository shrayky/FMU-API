using FmuApiDomain.LocalModule.Enums;

namespace FmuApiDomain.LocalModule.Models
{
    public class OrganizationLocalModuleState
    {
        public int Organization { get; set; } = 0;
        public LocalModuleStatus Status { get; set; } = LocalModuleStatus.Unknown;

        /// <summary>
        /// Признак того, что ЛМ инициализирован ТС ПИоТ: получен непустой, ещё не истёкший токен ТС ПИоТ.
        /// </summary>
        public bool InitializedByTsPiot { get; set; } = false;
    }
}
