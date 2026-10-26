namespace FmuApiDomain.Attributes;

/// <summary>
/// Поле настройки участвует в импорте и экспорте.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ExchangeableSettingAttribute : Attribute
{
}
