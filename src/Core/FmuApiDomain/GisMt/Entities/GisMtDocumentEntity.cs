using FmuApiDomain.Templates.Tables;

namespace FmuApiDomain.GisMt.Entities;

/// <summary>
/// Загруженный входящий документ ГИС МТ (для исключения повторной загрузки).
/// </summary>
public class GisMtDocumentEntity : IHaveStringId
{
    public string Id { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public DateTime DocDate { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string SenderInn { get; set; } = string.Empty;

    public string ReceiverInn { get; set; } = string.Empty;

    public string ProductGroup { get; set; } = string.Empty;

    public string OrganisationInn { get; set; } = string.Empty;

    public int MarksCount { get; set; }

    public DateTime LoadedAt { get; set; }
}
