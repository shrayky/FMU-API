using FmuApiDomain.GisMt.Entities;

namespace FmuApiDomain.GisMt.Models;

/// <summary>
/// Страница журнала документов ГИС МТ.
/// </summary>
public class GisMtJournalPage
{
    public List<GisMtJournalEntity> Documents { get; set; } = [];
    public int Count { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
