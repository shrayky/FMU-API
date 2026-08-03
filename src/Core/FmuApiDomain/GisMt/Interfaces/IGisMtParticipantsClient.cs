using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// HTTP-клиент TrueAPI для сведений об участниках оборота.
/// </summary>
public interface IGisMtParticipantsClient
{
    /// <summary>
    /// Получает сведения об участнике, включая товарные группы.
    /// </summary>
    Task<Result<List<ParticipantInfo>>> ParticipantsInfo(
        string token,
        string inn,
        CancellationToken cancellationToken = default);
}
