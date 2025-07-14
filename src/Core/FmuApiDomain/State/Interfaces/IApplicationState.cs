using FmuApiDomain.Authentication.Models;
using FmuApiDomain.LocalModule.Enums;

namespace FmuApiDomain.State.Interfaces
{
    public interface IApplicationState
    {
        void SetOnlineStatus(bool isOnline);
        bool IsOnline();
        TokenData TrueApiToken();
        void UpdateTrueApiToken(TokenData token);
        TokenData FmuToken();
        void UpdateFmuToken(TokenData token);
        LocalModuleStatus OrganizationLocalModuleStatus(int id);
        void UpdateOrganizationLocalModuleStatus(int organizationId, LocalModuleStatus status);
        bool WithoutOnlineCheck();
        void UpdateWithoutOnlineCheck(bool value);
    }
}
