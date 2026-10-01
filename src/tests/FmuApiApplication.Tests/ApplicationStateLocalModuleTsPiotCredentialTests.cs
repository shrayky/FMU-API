using FmuApiApplication.State;
using Xunit;

namespace FmuApiApplication.Tests;

public class ApplicationStateLocalModuleTsPiotCredentialTests
{
    [Fact]
    public void Токен_с_будущей_датой_истечения_читается_и_помечает_инициализацию_ТС_ПИоТ()
    {
        var state = new ApplicationState();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        state.UpdateLocalModuleTsPiotCredential(1, "token-1", expiresAt, "9999078900012345");

        var credential = state.LocalModuleTsPiotCredential(1);

        Assert.NotNull(credential);
        Assert.Equal("token-1", credential!.Token);
        Assert.Equal("9999078900012345", credential.FiscalDriveNumber);
        Assert.NotNull(credential.ExpiresAtUtc);
        Assert.Equal(DateTimeKind.Utc, credential.ExpiresAtUtc!.Value.Kind);
        Assert.True(credential.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void Местное_время_истечения_хранится_как_UTC()
    {
        var state = new ApplicationState();
        var expiresAtLocal = DateTime.Now.AddHours(1);

        state.UpdateLocalModuleTsPiotCredential(1, "token-1", expiresAtLocal, "9999078900012345");

        var credential = state.LocalModuleTsPiotCredential(1);

        Assert.NotNull(credential);
        Assert.Equal(DateTimeKind.Utc, credential!.ExpiresAtUtc!.Value.Kind);
        Assert.Equal(expiresAtLocal.ToUniversalTime(), credential.ExpiresAtUtc.Value);
    }

    [Fact]
    public void Просроченный_токен_не_читается()
    {
        var state = new ApplicationState();

        state.UpdateLocalModuleTsPiotCredential(1, "token-1", DateTime.UtcNow.AddHours(-1), "9999078900012345");

        Assert.Null(state.LocalModuleTsPiotCredential(1));
    }

    [Fact]
    public void Пустой_токен_очищает_сохранённое_значение()
    {
        var state = new ApplicationState();

        state.UpdateLocalModuleTsPiotCredential(1, "token-1", DateTime.UtcNow.AddHours(1), "9999078900012345");
        state.UpdateLocalModuleTsPiotCredential(1, string.Empty, null, string.Empty);

        Assert.Null(state.LocalModuleTsPiotCredential(1));
    }

    [Fact]
    public void Токен_без_даты_истечения_считается_действующим()
    {
        var state = new ApplicationState();

        state.UpdateLocalModuleTsPiotCredential(1, "token-1", null, "9999078900012345");

        var credential = state.LocalModuleTsPiotCredential(1);

        Assert.NotNull(credential);
        Assert.Equal("token-1", credential!.Token);
        Assert.Null(credential.ExpiresAtUtc);
    }

    [Fact]
    public void Код_организации_0_читает_токен_организации_1()
    {
        var state = new ApplicationState();

        state.UpdateLocalModuleTsPiotCredential(1, "token-1", DateTime.UtcNow.AddHours(1), "9999078900012345");

        var credential = state.LocalModuleTsPiotCredential(0);

        Assert.NotNull(credential);
        Assert.Equal("token-1", credential!.Token);
    }

    [Fact]
    public void Токен_по_коду_организации_0_доступен_организации_1()
    {
        var state = new ApplicationState();

        state.UpdateLocalModuleTsPiotCredential(0, "token-1", DateTime.UtcNow.AddHours(1), "9999078900012345");

        var credential = state.LocalModuleTsPiotCredential(1);

        Assert.NotNull(credential);
        Assert.Equal("token-1", credential!.Token);
    }
}
