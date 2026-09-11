using FmuApiApplication.State;
using Xunit;

namespace FmuApiApplication.Tests;

public class ApplicationStateTrueApiTokenTests
{
    [Fact]
    public void TrueApiToken_находит_токен_по_инн_с_пробелами()
    {
        var state = new ApplicationState();
        var expired = DateTime.Now.AddHours(1);
        state.UpdateTrueApiToken(" 246412218294 ", "token-1", expired);

        Assert.Equal("token-1", state.TrueApiToken("246412218294").Token);
        Assert.Equal("token-1", state.TrueApiToken(" 246412218294 ").Token);
    }
}
