using FmuApiDomain.Authentication.Models;
using FmuApiDomain.LocalModule.Enums;
using FmuApiDomain.LocalModule.Models;

namespace FmuApiDomain.State.Interfaces;

public interface IApplicationState
{
    void SetOnlineStatus(bool isOnline);
    bool IsOnline();
    void UpdateTrueApiToken(TokenData token);
    TokenData FmuToken();
    void UpdateFmuToken(TokenData token);
    LocalModuleStatus OrganizationLocalModuleStatus(int id);
    void UpdateOrganizationLocalModuleStatus(int organizationId, LocalModuleStatus status);
    void UpdateOrganizationLocalModuleInformation(int organizationId, LocalModuleState localModuleInfo);
    LocalModuleState LocalModuleInformation(int organizationId);
    bool WithoutOnlineCheck();
    void UpdateWithoutOnlineCheck(bool value);
    bool CouchDbOnline();
    void UpdateCouchDbState(bool value);
    void NeedRestartService(bool flag);
    bool NeedRestartService();
    void UpdateTrueApiToken(string inn, string token, DateTime lifeUntil);
    TokenData TrueApiToken();
    TokenData TrueApiToken(string inn);
}
