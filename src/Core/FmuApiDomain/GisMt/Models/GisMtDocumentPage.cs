using FmuApiDomain.GisMt.Entities;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Страница загруженных входящих документов УПД/УКД.
/// </summary>
public class GisMtDocumentPage
{
    public List<GisMtDocumentEntity> Documents { get; set; } = [];
    public int Count { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
