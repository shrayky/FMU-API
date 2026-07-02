using CSharpFunctionalExtensions;
using FmuApiDomain.Fmu.Document;
using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TrueApi.MarkData.Check;
using FmuApiDomain.TsPiot.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace TsPiotClinet.Services;

public class TsPiotFabricService : ITsPiotService
{
    private readonly IServiceProvider _services;
    private readonly IApplicationState _applicationState;

    public TsPiotFabricService(IServiceProvider services, IApplicationState applicationState)
    {
        _services = services;
        _applicationState = applicationState;
    }

    public Task<Result<CheckMarksDataTrueApi>> Check(string mark, TsPiotConnectionSettings connectionSettings)
        => Service($"{connectionSettings.Host}:{connectionSettings.Port}").Check(mark, connectionSettings);

    private ITsPiotService Service(string connection)
        => _services.GetRequiredKeyedService<ITsPiotService>(_applicationState.TsPiotApiVersion(connection));
}
