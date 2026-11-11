using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Синхронизация входящих документов УПД/УКД из ГИС МТ.
/// </summary>
public interface IGisMtDocumentsSyncService
{
    /// <summary>
    /// Синхронизирует входящие документы за настроенный период (DocumentsSyncDays).
    /// </summary>
    Task<Result<GisMtDocumentsSyncResult>> Sync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Заново загружает doc/info одного документа и сохраняет его марки.
    /// </summary>
    Task<Result<int>> ReloadDocument(string number, CancellationToken cancellationToken = default);
}
