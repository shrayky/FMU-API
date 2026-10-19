using CSharpFunctionalExtensions;
using FmuApiDomain.TrueApiIntegration;

namespace TrueApiIntegration.Interfaces;

/// <summary>
/// Получает UUID-токен True API подписью КриптоПро.
/// </summary>
public interface IAuthService
{
    Task<Result<TrueApiSession>> GenerateToken(string inn, string password, string signatureNumber);
}

