using System.Globalization;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;

namespace FmuApiDomain.TrueApiIntegration;

/// <summary>
/// Запрос единого UUID-токена True API и разбор ответа simpleSignIn.
/// </summary>
public static class TrueApiUnitedSignIn
{
    public static TrueApiSignInBody Body(string uuid, string signedData, string? inn)
    {
        var normalizedInn = (inn ?? string.Empty).Trim();
        return new TrueApiSignInBody
        {
            Uuid = uuid,
            Data = signedData,
            Inn = normalizedInn.Length == 0 ? null : normalizedInn,
            UnitedToken = true
        };
    }

    public static Result<TrueApiSession> Parse(string? uuidToken, string? expireDate)
    {
        var token = (uuidToken ?? string.Empty).Trim();
        if (token.Length == 0)
            return Result.Failure<TrueApiSession>("True API не вернул токен UUID");

        if (!Guid.TryParse(token, out _))
            return Result.Failure<TrueApiSession>("True API вернул uuidToken не в формате UUID");

        var rawExpire = (expireDate ?? string.Empty).Trim();
        if (rawExpire.Length == 0)
            return Result.Failure<TrueApiSession>("True API не вернул срок действия токена");

        if (!DateTimeOffset.TryParse(
                rawExpire,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expires))
            return Result.Failure<TrueApiSession>("True API вернул некорректный срок действия токена");

        return Result.Success(new TrueApiSession(token, expires.LocalDateTime));
    }
}

/// <summary>
/// Тело simpleSignIn с запросом единого токена.
/// </summary>
public sealed class TrueApiSignInBody
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public string Data { get; init; } = string.Empty;

    [JsonPropertyName("inn")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Inn { get; init; }

    [JsonPropertyName("unitedToken")]
    public bool UnitedToken { get; init; }
}
