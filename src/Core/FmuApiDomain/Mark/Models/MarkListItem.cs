using FmuApiDomain.Mark.Entities;
using FmuApiDomain.Mark.Enums;

namespace FmuApiDomain.Mark.Models;

public class MarkListItem
{
    public string Id { get; set; } = string.Empty;
    public string MarkId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public TrueApiAnswerData TrueApiAnswerProperties { get; set; } = new();
    public bool HaveTrueApiAnswer { get; set; }
    public string CheckId { get; set; } = string.Empty;
    public MarkCheckSource CheckSource { get; set; } = MarkCheckSource.Undefined;

    public static MarkListItem FromEntity(MarkEntity mark) => new()
    {
        Id = mark.Id,
        MarkId = mark.MarkId,
        State = mark.State,
        TrueApiAnswerProperties = mark.TrueApiAnswerProperties,
        HaveTrueApiAnswer = mark.HaveTrueApiAnswer
    };
}
