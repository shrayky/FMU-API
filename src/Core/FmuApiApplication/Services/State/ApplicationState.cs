using FmuApiDomain.Authentication.Models;
using FmuApiDomain.LocalModule.Enums;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApiIntegration.Models;

namespace FmuApiApplication.Services.State;

public class ApplicationState : IApplicationState
{
    private bool _online { get; set; } = true;
    private TokenData _trueApiToken { get; set; } = new();
    private TokenData _fmuToken { get; set; } = new();
    private List<OrganizationLocalModuleState> _localModules { get; set; } = [];
    private Dictionary<int, LocalModuleState> _localModuleInformation { get; set; } = [];
    private bool _withoutOnlineCheck { get; set; } = false;
    private bool _couchDbIsOnline { get; set; } = false;
    private bool _needRestartService { get; set; } = false;
    private List<TrueApiToken> _trueApiTokens { get; set; } = [];

    public ApplicationState()
    {
        var args = Environment.GetCommandLineArgs();
        _withoutOnlineCheck = args.Contains("--noOnlineCheck");
    }

    public void SetOnlineStatus(bool isOnline)
    {
        if (_online != isOnline)
            _online = isOnline;
    }

    public void UpdateTrueApiToken(TokenData token)
    {
        _trueApiToken = token;
    }

    public bool WithoutOnlineCheck()
    {
        return _withoutOnlineCheck;
    }

    public void UpdateWithoutOnlineCheck(bool value)
    {
        _withoutOnlineCheck = value;
    }

    public void UpdateFmuToken(TokenData token)
    {
        _fmuToken = token;
    }

    public bool IsOnline()
    {
        return _online;
    }

    public TokenData FmuToken()
    {
        return _fmuToken;
    }

    public LocalModuleStatus OrganizationLocalModuleStatus(int organizationId)
    {
        organizationId = organizationId == 0 ? 1 : organizationId;

        var lmStatusInfo = _localModules.FirstOrDefault(p => p.Organization == organizationId);

        if (lmStatusInfo == null)
            return LocalModuleStatus.Unknown;

        return lmStatusInfo.Status;
    }

    public void UpdateOrganizationLocalModuleStatus(int organizationId, LocalModuleStatus status)
    {
        var lmStatusInfo = _localModules.FirstOrDefault(p => p.Organization == organizationId);

        if (lmStatusInfo == null) {
            lmStatusInfo = new()
            {
                Organization = organizationId,
                Status = status
            };

            _localModules.Add(lmStatusInfo);
        }
        else
            lmStatusInfo.Status = status;
    }

    public void UpdateOrganizationLocalModuleInformation(int organizationId, LocalModuleState localModuleInfo)
    {
        _localModuleInformation.Remove(organizationId);

        _localModuleInformation.Add(organizationId, localModuleInfo);
    }

    public LocalModuleState LocalModuleInformation(int organizationId)
    {
        LocalModuleState lmInfo = new();

        _localModuleInformation.TryGetValue(organizationId, out lmInfo);

        return lmInfo;
    }

    public bool CouchDbOnline()
    {
        return _couchDbIsOnline;
    }

    public void UpdateCouchDbState(bool value)
    {
        _couchDbIsOnline = value;
    }

    public void NeedRestartService(bool flag)
    {
        _needRestartService = flag;
    }

    public bool NeedRestartService()
    {
        return _needRestartService;
    }

    public void UpdateTrueApiToken(string inn, string token, DateTime lifeUntil)
    {
        var organisationTokenData = _trueApiTokens.FirstOrDefault(p => p.Inn == inn);

        if (organisationTokenData != null)
        {
            _trueApiTokens.Remove(organisationTokenData);
        }

        organisationTokenData = new()
        {
            Inn = inn,
            Token = token,
            LiveUntil = lifeUntil,
        };

        _trueApiTokens.Add(organisationTokenData);
    }

    public TokenData TrueApiToken(string inn)
    {
        var tokenData = _trueApiTokens.FirstOrDefault(p => p.Inn == inn);

        if (tokenData == null)
            return new();

        if (tokenData.LiveUntil < DateTime.Now)
            return new();

        return new(tokenData.Token, tokenData.LiveUntil); ;
    }

    public TokenData TrueApiToken()
    {
        foreach (var tokenData in _trueApiTokens)
        {
            if (tokenData.LiveUntil > DateTime.Now)
                return new(tokenData.Token, tokenData.LiveUntil);
        }

        return new();
    }
}
