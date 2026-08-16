using FmuApiDomain.State.Interfaces;
using FmuApiDomain.TsPiot.Models;

namespace TsPiotClinet.Services;

internal static class TsPiotLastCheckStatusWriter
{
    /// <summary>
    /// Сохраняет HTTP-код последнего обращения к ТС ПИоТ.
    /// </summary>
    public static void Save(IApplicationState applicationState, TsPiotConnectionSettings connection, int statusCode)
    {
        applicationState.UpdateTsPiotLastCheckStatusCode(
            $"{connection.Host}:{connection.Port}",
            statusCode);
    }
}
