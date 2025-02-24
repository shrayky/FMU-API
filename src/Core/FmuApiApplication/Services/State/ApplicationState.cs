using FmuApiDomain.Authentication.Models;
using FmuApiDomain.LocalModule.Enums;
using FmuApiDomain.LocalModule.Models;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.Services.State
{
    public class ApplicationState : IApplicationState
    {
        private bool _online { get; set; } = true;
        private TokenData _trueApiToken { get; set; } = new();
        private TokenData _fmuToken { get; set; } = new();
        private List<OrganizationLocalModuleState> _localModules { get; set; } = new();
        private bool _withoutOnlineCheck { get; set; } = false;

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

        public TokenData TrueApiToken()
        {
            return _trueApiToken;
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

    }
}
