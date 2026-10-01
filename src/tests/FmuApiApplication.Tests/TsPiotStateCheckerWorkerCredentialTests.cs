using FmuApiApplication.State;
using FmuApiDomain.Configuration.Options.Organization;
using FmuApiDomain.TsPiot.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Json;
using System.Net;
using TsPiotClinet.Models;
using TsPiotClinet.Workers;
using Xunit;

namespace FmuApiApplication.Tests;

/// <summary>
/// Проверяет, что недоступный ТС ПИоТ не снимает сохранённый токен:
/// очищать его можно только тогда, когда info ответил и <c>lm.token</c> в ответе пустой.
/// </summary>
public class TsPiotStateCheckerWorkerCredentialTests
{
    private const string InfoWithToken = """
        {"tspiotId":"1","fnSerial":"9999078900012345","lm":{"token":"3f1a7c2e-0000-4444-8888-000000000001","expDate":"2030-01-01T00:00:00Z"}}
        """;

    private const string InfoWithoutToken = """
        {"tspiotId":"1","fnSerial":"9999078900012345","lm":{"token":"","expDate":""}}
        """;

    [Fact]
    public void Разобранный_ответ_с_токеном_сохраняет_токен()
    {
        var state = new ApplicationState();
        var worker = Worker(state);

        worker.ApplyTsPiotInfoAnswer(PrintGroup(), isAnswered: true, KktInfo(InfoWithToken));

        var credential = state.LocalModuleTsPiotCredential(1);

        Assert.NotNull(credential);
        Assert.Equal("3f1a7c2e-0000-4444-8888-000000000001", credential!.Token);
        Assert.Equal("9999078900012345", credential.FiscalDriveNumber);
    }

    [Fact]
    public void Разобранный_ответ_с_пустым_токеном_очищает_токен()
    {
        var state = new ApplicationState();
        state.UpdateLocalModuleTsPiotCredential(1, "old-token", DateTime.UtcNow.AddHours(1), "9999078900012345");

        var worker = Worker(state);

        worker.ApplyTsPiotInfoAnswer(PrintGroup(), isAnswered: true, KktInfo(InfoWithoutToken));

        Assert.Null(state.LocalModuleTsPiotCredential(1));
    }

    [Fact]
    public void Ответ_без_тела_не_очищает_токен()
    {
        var state = new ApplicationState();
        state.UpdateLocalModuleTsPiotCredential(1, "old-token", DateTime.UtcNow.AddHours(1), "9999078900012345");

        var worker = Worker(state);

        worker.ApplyTsPiotInfoAnswer(PrintGroup(), isAnswered: false, kktInfo: null);

        Assert.NotNull(state.LocalModuleTsPiotCredential(1));
    }

    [Fact]
    public void Неразобранное_info_не_очищает_токен()
    {
        var state = new ApplicationState();
        state.UpdateLocalModuleTsPiotCredential(1, "old-token", DateTime.UtcNow.AddHours(1), "9999078900012345");

        var worker = Worker(state);

        worker.ApplyTsPiotInfoAnswer(PrintGroup(), isAnswered: false, KktInfo(InfoWithoutToken));

        Assert.NotNull(state.LocalModuleTsPiotCredential(1));
    }

    [Fact]
    public async Task Таймаут_info_не_считается_ответом()
    {
        var worker = Worker(new ApplicationState(), new ThrowingHandler());

        var answer = await worker.AskProtocolVersion(TsPiotSettings());

        Assert.False(answer.IsAnswered);
        Assert.True(answer.Protocol.IsFailure);
    }

    [Fact]
    public async Task Ответ_5xx_не_считается_ответом()
    {
        var worker = Worker(new ApplicationState(), new StatusHandler(HttpStatusCode.InternalServerError, string.Empty));

        var answer = await worker.AskProtocolVersion(TsPiotSettings());

        Assert.False(answer.IsAnswered);
        Assert.Equal(500, answer.StatusCode);
    }

    [Fact]
    public async Task Пустое_тело_ответа_не_считается_ответом()
    {
        var worker = Worker(new ApplicationState(), new StatusHandler(HttpStatusCode.OK, string.Empty));

        var answer = await worker.AskProtocolVersion(TsPiotSettings());

        Assert.False(answer.IsAnswered);
    }

    [Fact]
    public async Task Разобранное_info_считается_ответом()
    {
        var worker = Worker(new ApplicationState(), new StatusHandler(HttpStatusCode.OK, InfoWithToken));

        var answer = await worker.AskProtocolVersion(TsPiotSettings());

        Assert.True(answer.IsAnswered);
        Assert.Equal(3, answer.Protocol.Value);
        Assert.Equal("9999078900012345", answer.KktInfo!.FnSerialNumber);
    }

    private static TsPiotKktInfo KktInfo(string json)
    {
        return JsonHelpers.DeserializeAsync<TsPiotKktInfo>(json).AsTask().GetAwaiter().GetResult()!;
    }

    private static PrintGroupData PrintGroup()
    {
        return new PrintGroupData
        {
            Id = 1,
            Name = "Тестовая организация",
            TsPiot = TsPiotSettings()
        };
    }

    private static TsPiotConnectionSettings TsPiotSettings()
    {
        return new TsPiotConnectionSettings
        {
            Host = "http://127.0.0.1",
            Port = "5995"
        };
    }

    private static TsPiotStateCheckerWorker Worker(ApplicationState state, HttpMessageHandler? handler = null)
    {
        var factory = new SingleClientFactory(handler ?? new StatusHandler(HttpStatusCode.OK, InfoWithToken));

        return new TsPiotStateCheckerWorker(
            NullLogger<TsPiotStateCheckerWorker>.Instance,
            parametersService: null!,
            applicationState: state,
            httpClientFactory: factory,
            tsPiotEspApiService: null!,
            piotSettingsService: null!);
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StatusHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("ТС ПИоТ недоступен");
        }
    }
}
