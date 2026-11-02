namespace FmuApiDomain.SoftwareUpdate;

/// Итог получения пакетов: сколько файлов записано и какие файлы не скачались.
public sealed class ClientPackageRefreshResult
{
    public ClientPackageRefreshResult(int downloaded, IReadOnlyList<string> errors)
    {
        Downloaded = downloaded;
        Errors = errors;
    }

    public int Downloaded { get; }

    public IReadOnlyList<string> Errors { get; }
}
