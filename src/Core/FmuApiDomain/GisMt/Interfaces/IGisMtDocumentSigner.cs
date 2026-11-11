using CSharpFunctionalExtensions;

namespace FmuApiDomain.GisMt.Interfaces;

/// <summary>
/// Откреплённая подпись документа ГИС МТ сертификатом организации.
/// </summary>
public interface IGisMtDocumentSigner
{
    Result<string> SignDetached(string inn, string certificateNumber, string password, string payload);
}
