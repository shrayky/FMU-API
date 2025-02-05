using FmuApiDomain.Authentication.Models;
using FmuApiDomain.State.Interfaces;

namespace FmuApiApplication.Services.State
{
    public class ApplicationState : IApplicationState
    {
        private bool _online { get; set; } = true;
        private TokenData _trueApiToken { get; set; } = new();
        private TokenData _fmuToken { get; set; } = new();

        public void SetOnlineStatus(bool isOnline)
        {
            if (_online != isOnline)
                _online = isOnline;
        }

        public void UpdateTrueApiToken(TokenData token)
        {
            _trueApiToken = token;
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
    }
}
