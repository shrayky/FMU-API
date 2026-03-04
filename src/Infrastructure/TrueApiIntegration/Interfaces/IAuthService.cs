namespace TrueApiIntegration.Interfaces;

public interface IAuthService
{
    Task<string> GenerateToken(string inn, string password);
}

