using CSharpFunctionalExtensions;

namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Выбор реквизитов участника по длине ИНН: 10 цифр — юридическое лицо с КПП, 12 — ИП с ФИАС.
/// </summary>
public static class GisMtParticipantRules
{
    public const int LegalInnLength = 10;
    public const int IndividualInnLength = 12;

    public static Result<GisMtParticipant> Resolve(string? inn, string? kpp, string? fiasId) =>
        Resolve(inn, kpp, fiasId, modRequired: true);

    /// <summary>
    /// Реквизиты участника; при <paramref name="modRequired"/> = false КПП и ФИАС не проверяются и в документ не попадают.
    /// </summary>
    public static Result<GisMtParticipant> Resolve(string? inn, string? kpp, string? fiasId, bool modRequired)
    {
        var normalizedInn = (inn ?? string.Empty).Trim();
        var normalizedKpp = (kpp ?? string.Empty).Trim();
        var normalizedFias = (fiasId ?? string.Empty).Trim();

        if (normalizedInn.Length == LegalInnLength)
        {
            if (modRequired && normalizedKpp.Length == 0)
                return Result.Failure<GisMtParticipant>($"Для ИНН {normalizedInn} не заполнен КПП организации");

            return Result.Success(new GisMtParticipant(
                normalizedInn,
                false,
                modRequired ? normalizedKpp : string.Empty,
                string.Empty));
        }

        if (normalizedInn.Length == IndividualInnLength)
        {
            if (modRequired && normalizedFias.Length == 0)
                return Result.Failure<GisMtParticipant>($"Для ИНН {normalizedInn} не заполнен ФИАС организации");

            return Result.Success(new GisMtParticipant(
                normalizedInn,
                true,
                string.Empty,
                modRequired ? normalizedFias : string.Empty));
        }

        return Result.Failure<GisMtParticipant>(
            $"ИНН организации должен содержать {LegalInnLength} цифр у юридического лица или {IndividualInnLength} у ИП, а содержит {normalizedInn.Length}");
    }
}
