using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options;

namespace FmuApiDomain.Frontol.Interfaces;

/// <summary>
/// Проверка доступности базы Frontol по параметрам подключения Firebird.
/// </summary>
public interface IFrontolConnectionProbe
{
    Task<Result> Probe(FrontolConnectionSettings connection, CancellationToken cancellationToken);
}
