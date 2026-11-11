namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Реквизиты участника оборота для тела документа ГИС МТ.
/// </summary>
public sealed record GisMtParticipant(string Inn, bool IsIndividual, string Kpp, string FiasId);
