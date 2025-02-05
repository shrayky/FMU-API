using FmuApiDomain.Authentication.Models;

namespace FmuApiDomain.State.Interfaces
{
    public interface IApplicationState
    {
        void SetOnlineStatus(bool isOnline);
        bool IsOnline();
        TokenData TrueApiToken();
        void UpdateTrueApiToken(TokenData token);
        TokenData FmuToken();
        public void UpdateFmuToken(TokenData token);
    }
}
