using CSharpFunctionalExtensions;
using FmuApiDomain.GisMt.Models;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// HTTP-клиент True API: содержимое чека ККТ.
/// </summary>
public interface IGisMtReceiptClient
{
    Task<Result<GisMtReceiptInfo>> ReceiptInfo(
        string token,
        string receiptId,
        CancellationToken cancellationToken = default);
}
