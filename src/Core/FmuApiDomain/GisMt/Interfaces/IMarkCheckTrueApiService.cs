using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

public interface IMarkCheckTrueApiService
{
    Task<MarkCheckTrueApiResult> GetCisesInfo(
        string inn,
        IReadOnlyList<string> cises,
        CancellationToken cancellationToken = default);
}
