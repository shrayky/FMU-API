using FmuApiDomain.Mark.Enums;

namespace FmuApiDomain.Mark.Models;

/// <summary>
/// Идентификатор и источник последней сохранённой проверки марки.
/// </summary>
public class LastMarkCheck
{
    public string Id { get; set; } = string.Empty;
    public MarkCheckSource CheckSource { get; set; } = MarkCheckSource.Undefined;
}
