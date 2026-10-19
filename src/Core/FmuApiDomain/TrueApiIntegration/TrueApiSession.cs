namespace FmuApiDomain.TrueApiIntegration;

/// <summary>
/// Сессия True API: UUID-токен и срок в локальном времени.
/// </summary>
public sealed record TrueApiSession(string Token, DateTime LiveUntil);
