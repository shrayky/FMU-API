using CSharpFunctionalExtensions;

namespace FmuApiDomain.SoftwareUpdate.Interfaces;

/// Установка скачанного архива обновления. Реализация живёт во внешнем контуре.
public interface ISoftwarePackageInstaller
{
    /// Распаковывает архив и запускает установку. sha256 — контрольная сумма пакета.
    Task<Result> InstallAsync(string zipPath, string sha256);
}
