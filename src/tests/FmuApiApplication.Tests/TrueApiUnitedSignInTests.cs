using System.Text.Json;
using FmuApiDomain.TrueApiIntegration;
using Xunit;

namespace FmuApiApplication.Tests;

public class TrueApiUnitedSignInTests
{
    [Fact]
    public void Parse_берёт_uuid_и_локальный_срок()
    {
        var result = TrueApiUnitedSignIn.Parse(
            "123e4567-e89b-12d3-a456-426655440000",
            "2026-10-10T00:00:00.123Z");

        Assert.True(result.IsSuccess);
        Assert.Equal("123e4567-e89b-12d3-a456-426655440000", result.Value.Token);
        Assert.Equal(
            DateTimeOffset.Parse("2026-10-10T00:00:00.123Z").LocalDateTime,
            result.Value.LiveUntil);
        Assert.Equal(DateTimeKind.Local, result.Value.LiveUntil.Kind);
    }

    [Fact]
    public void Parse_без_uuidToken_отказ()
    {
        var result = TrueApiUnitedSignIn.Parse(null, "2026-10-10T00:00:00.123Z");

        Assert.True(result.IsFailure);
        Assert.Equal("True API не вернул токен UUID", result.Error);
    }

    [Fact]
    public void Parse_jwt_вместо_uuid_отказ()
    {
        var result = TrueApiUnitedSignIn.Parse(
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e.mk",
            "2026-10-10T00:00:00.123Z");

        Assert.True(result.IsFailure);
        Assert.Equal("True API вернул uuidToken не в формате UUID", result.Error);
    }

    [Fact]
    public void Parse_без_срока_отказ()
    {
        var result = TrueApiUnitedSignIn.Parse("123e4567-e89b-12d3-a456-426655440000", " ");

        Assert.True(result.IsFailure);
        Assert.Equal("True API не вернул срок действия токена", result.Error);
    }

    [Fact]
    public void Parse_битый_срок_отказ()
    {
        var result = TrueApiUnitedSignIn.Parse("123e4567-e89b-12d3-a456-426655440000", "завтра");

        Assert.True(result.IsFailure);
        Assert.Equal("True API вернул некорректный срок действия токена", result.Error);
    }

    [Fact]
    public void Body_запрашивает_единый_токен_и_не_шлёт_пустой_inn()
    {
        var withInn = TrueApiUnitedSignIn.Body("uuid-1", "c2ln", "7700000000");
        var withoutInn = TrueApiUnitedSignIn.Body("uuid-1", "c2ln", "");

        var json = JsonSerializer.Serialize(withInn);
        Assert.Contains("\"unitedToken\":true", json);
        Assert.Contains("\"uuid\":\"uuid-1\"", json);
        Assert.Contains("\"data\":\"c2ln\"", json);
        Assert.Contains("\"inn\":\"7700000000\"", json);

        var emptyInn = JsonSerializer.Serialize(withoutInn);
        Assert.DoesNotContain("inn", emptyInn);
        Assert.Contains("\"unitedToken\":true", emptyInn);
    }
}
