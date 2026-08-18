namespace FmuApiDomain.Configuration.Options;

/// <summary>
/// Соответствие кода товарной группы Атол коду Честного знака (ГИС МТ).
/// </summary>
public class GisMtProductMapping
{
    public int AtolCode { get; set; }

    public int TrueApiGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool CheckSmp { get; set; }
}
