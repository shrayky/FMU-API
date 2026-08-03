using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// HTTP-клиент TrueAPI для документов ГИС МТ.
/// </summary>
public interface IGisMtDocumentsClient
{
    /// <summary>
    /// Получает список документов за период.
    /// </summary>
    Task<Result<GisMtDocListResponse>> DocumentList(
        string token,
        string productGroup,
        string receiverInn,
        DateTime dateFrom,
        DateTime dateTo,
        IEnumerable<string> documentTypes,
        string? did,
        string? orderedColumnValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает содержимое документа по идентификатору.
    /// </summary>
    Task<Result<GisMtDocInfoResponse>> DocumentInfo(
        string token,
        string documentId,
        string? productGroup,
        CancellationToken cancellationToken = default);
}
