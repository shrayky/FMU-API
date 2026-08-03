namespace FmuApiDomain.GisMt;

/// <summary>
/// Константы типов документов УПД/УКД для загрузки из ГИС МТ.
/// </summary>
public static class GisMtDocumentTypes
{
    public static readonly string[] UpdUkd =
    [
        "UNIVERSAL_TRANSFER_DOCUMENT",
        "UNIVERSAL_TRANSFER_DOCUMENT_FIX",
        "UNIVERSAL_CORRECTION_DOCUMENT",
        "UNIVERSAL_CORRECTION_DOCUMENT_FIX"
    ];
}
