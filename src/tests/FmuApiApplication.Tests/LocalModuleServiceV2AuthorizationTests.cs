using FmuApiDomain.Configuration.Options;
using FmuApiDomain.LocalModule.Models;
using LocalModuleIntegration.Service;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace FmuApiApplication.Tests;

/// <summary>
/// Проверяет авторизацию запроса проверки КИ в локальном модуле версии 2:
/// с токеном ТС ПИоТ — Token и X-ClientId, без токена — Basic без X-ClientId.
/// </summary>
public class LocalModuleServiceV2AuthorizationTests
{
    private const string ResponseContent = """
        {"results":[{"code":0,"description":"ok","reqId":"1","reqTimestamp":1,"inst":"i","version":"2"}]}
        """;

    [Fact]
    public async Task С_токеном_используется_схема_Token_и_заголовок_X_ClientId()
    {
        var handler = new CapturingHandler(ResponseContent);
        var service = CreateService(handler);

        await service.OutCheckAsync(Connection(), "0104650072583993215)EEEE", "x-api-key", 1,
            new LocalModuleCheckAuthorization("3f1a7c2e-0000-4444-8888-000000000001", "9999078900012345"));

        var request = Assert.Single(handler.Requests);

        Assert.NotNull(request.Authorization);
        Assert.Equal("Token", request.Authorization!.Scheme);
        Assert.Equal("3f1a7c2e-0000-4444-8888-000000000001", request.Authorization.Parameter);
        Assert.Equal("9999078900012345", request.ClientId);
    }

    [Fact]
    public async Task Без_токена_используется_Basic()
    {
        var handler = new CapturingHandler(ResponseContent);
        var service = CreateService(handler);

        await service.OutCheckAsync(Connection(), "0104650072583993215)EEEE", "x-api-key", 1,
            new LocalModuleCheckAuthorization(null, null));

        var request = Assert.Single(handler.Requests);

        Assert.NotNull(request.Authorization);
        Assert.Equal("Basic", request.Authorization!.Scheme);
        Assert.Null(request.ClientId);
    }

    [Fact]
    public async Task Пустой_номер_ФН_не_добавляет_заголовок_X_ClientId()
    {
        var handler = new CapturingHandler(ResponseContent);
        var service = CreateService(handler);

        await service.OutCheckAsync(Connection(), "0104650072583993215)EEEE", "x-api-key", 1,
            new LocalModuleCheckAuthorization("3f1a7c2e-0000-4444-8888-000000000001", "  "));

        var request = Assert.Single(handler.Requests);

        Assert.Equal("Token", request.Authorization!.Scheme);
        Assert.Null(request.ClientId);
    }

    [Fact]
    public async Task Пустой_токен_не_подменяет_Basic()
    {
        var handler = new CapturingHandler(ResponseContent);
        var service = CreateService(handler);

        await service.OutCheckAsync(Connection(), "0104650072583993215)EEEE", "x-api-key",
            authorization: new LocalModuleCheckAuthorization("   ", null));

        var request = Assert.Single(handler.Requests);

        Assert.Equal("Basic", request.Authorization!.Scheme);
        Assert.Null(request.ClientId);
    }

    [Fact]
    public async Task Адрес_запроса_остаётся_outCheck_версии_2()
    {
        var handler = new CapturingHandler(ResponseContent);
        var service = CreateService(handler);

        await service.OutCheckAsync(Connection(), "0104650072583993215)EEEE", "x-api-key");

        var request = Assert.Single(handler.Requests);

        Assert.Equal("/api/v2/cis/outCheck", request.RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, request.Method);
    }

    private static LocalModuleServiceV2 CreateService(CapturingHandler handler)
    {
        return new LocalModuleServiceV2(
            new SingleClientFactory(handler),
            NullLogger<LocalModuleServiceV1>.Instance);
    }

    private static LocalModuleConnection Connection()
    {
        return new LocalModuleConnection
        {
            Enable = true,
            ConnectionAddress = "http://127.0.0.1:5995",
            UserName = "user",
            Password = "password"
        };
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class CapturingHandler(string content) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var clientId = request.Headers.TryGetValues("X-ClientId", out var values)
                ? values.FirstOrDefault()
                : null;

            Requests.Add(new CapturedRequest(request.Method, request.RequestUri, request.Headers.Authorization, clientId));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        }
    }

    private sealed record CapturedRequest(HttpMethod Method, Uri? RequestUri, AuthenticationHeaderValue? Authorization, string? ClientId);
}
