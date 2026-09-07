namespace FmuApiDomain.Mark.Enums;

/// <summary>
/// Способ, которым была выполнена проверка марки.
/// </summary>
public enum MarkCheckSource
{
    Undefined = 0,
    OnlineXApiKey = 1,
    OnlineTsPiot = 2,
    LocalModule = 3,
    Database = 4
}
