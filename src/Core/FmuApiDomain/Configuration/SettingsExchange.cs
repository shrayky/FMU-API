using FmuApiDomain.Attributes;
using Shared.Json;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FmuApiDomain.Configuration;

/// <summary>
/// Импорт и экспорт настроек, помеченных <see cref="ExchangeableSettingAttribute"/>.
/// Поле без атрибута не переносится: у организации остаётся интеграция с КриптоПро.
/// </summary>
public static class SettingsExchange
{
    private static readonly JsonSerializerOptions Options = JsonSerializeOptionsProvider.Default();

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ExchangeableByType = new();

    public static string Export(Parameters source)
    {
        var node = WriteSelective(source, typeof(Parameters));
        return node?.ToJsonString(Options) ?? "{}";
    }

    public static bool TryApply(Parameters target, string json, out bool changed, out string? error)
    {
        changed = false;
        error = null;

        try
        {
            if (JsonNode.Parse(json) is not JsonObject root)
            {
                error = "Ожидается объект JSON";
                return false;
            }

            var clone = Clone(target);
            var touched = new List<PropertyInfo>();

            foreach (var property in ExchangeableProperties(typeof(Parameters)))
            {
                if (!TryGetNode(root, JsonName(property), out var node))
                    continue;

                ApplyValue(clone, property, node);
                touched.Add(property);
            }

            foreach (var property in touched)
                property.SetValue(target, property.GetValue(clone));

            changed = touched.Count > 0;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            changed = false;
            return false;
        }
    }

    private static Parameters Clone(Parameters source)
    {
        var json = JsonSerializer.Serialize(source, Options);
        return JsonSerializer.Deserialize<Parameters>(json, Options)
            ?? throw new InvalidOperationException("Не удалось скопировать текущие настройки");
    }

    private static JsonNode? WriteSelective(object source, Type type)
    {
        var obj = new JsonObject();

        foreach (var property in ExchangeableProperties(type))
        {
            var value = property.GetValue(source);
            if (value is null)
                continue;

            obj[JsonName(property)] = WriteValue(value, property.PropertyType);
        }

        return obj;
    }

    private static JsonNode? WriteValue(object value, Type type)
    {
        if (IsList(type, out var elementType) && HasExchangeable(elementType))
        {
            var array = new JsonArray();
            foreach (var item in (IEnumerable)value)
            {
                if (item is null)
                    continue;

                array.Add(WriteSelective(item, elementType));
            }

            return array;
        }

        if (IsComplex(type) && HasExchangeable(type))
            return WriteSelective(value, type);

        return JsonSerializer.SerializeToNode(value, type, Options);
    }

    private static void ApplyValue(object target, PropertyInfo property, JsonNode? node)
    {
        var type = property.PropertyType;

        if (node is null)
            return;

        if (IsList(type, out var elementType) && HasExchangeable(elementType))
        {
            if (node is not JsonArray array)
                throw new InvalidOperationException($"Поле {property.Name} должно быть массивом");

            property.SetValue(target, ApplySelectiveList(property.GetValue(target), array, elementType));
            return;
        }

        if (IsComplex(type) && HasExchangeable(type))
        {
            if (node is not JsonObject childJson)
                throw new InvalidOperationException($"Поле {property.Name} должно быть объектом");

            var child = property.GetValue(target) ?? Activator.CreateInstance(type)
                ?? throw new InvalidOperationException($"Не удалось создать {type.Name}");

            ApplySelectiveObject(child, childJson, type);
            property.SetValue(target, child);
            return;
        }

        property.SetValue(target, node.Deserialize(type, Options));
    }

    private static void ApplySelectiveObject(object target, JsonObject json, Type type)
    {
        foreach (var property in ExchangeableProperties(type))
        {
            if (!TryGetNode(json, JsonName(property), out var node))
                continue;

            ApplyValue(target, property, node);
        }
    }

    private static IList ApplySelectiveList(object? current, JsonArray array, Type elementType)
    {
        var existing = new List<object>();
        if (current is IEnumerable currentItems)
        {
            foreach (var item in currentItems)
            {
                if (item is not null)
                    existing.Add(item);
            }
        }

        var idProperty = elementType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

        foreach (var itemNode in array)
        {
            if (itemNode is not JsonObject itemJson)
                throw new InvalidOperationException($"Элемент {elementType.Name} должен быть объектом");

            var item = Activator.CreateInstance(elementType)
                ?? throw new InvalidOperationException($"Не удалось создать {elementType.Name}");

            ApplySelectiveObject(item, itemJson, elementType);
            KeepSilentFields(item, FindById(existing, item, idProperty), elementType, itemJson);
            list.Add(item);
        }

        return list;
    }

    private static void KeepSilentFields(object item, object? match, Type elementType, JsonObject fileItem)
    {
        if (match is null)
            return;

        foreach (var property in elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanWrite || property.GetIndexParameters().Length > 0)
                continue;

            var marked = property.GetCustomAttribute<ExchangeableSettingAttribute>() != null;
            if (marked && TryGetNode(fileItem, JsonName(property), out _))
                continue;

            property.SetValue(item, property.GetValue(match));
        }
    }

    private static object? FindById(List<object> items, object candidate, PropertyInfo? idProperty)
    {
        if (idProperty is null)
            return null;

        var id = idProperty.GetValue(candidate);
        foreach (var item in items)
        {
            if (Equals(idProperty.GetValue(item), id))
                return item;
        }

        return null;
    }

    private static bool HasExchangeable(Type type) => ExchangeableProperties(type).Length > 0;

    private static PropertyInfo[] ExchangeableProperties(Type type)
    {
        return ExchangeableByType.GetOrAdd(type, static key => key
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead
                && property.CanWrite
                && property.GetIndexParameters().Length == 0
                && property.GetCustomAttribute<ExchangeableSettingAttribute>() != null)
            .ToArray());
    }

    private static bool IsList(Type type, out Type elementType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = type;
        return false;
    }

    private static bool IsComplex(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(string) || underlying.IsPrimitive || underlying.IsEnum)
            return false;

        if (underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid))
            return false;

        return underlying.IsClass;
    }

    private static string JsonName(PropertyInfo property)
    {
        var named = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return named?.Name ?? property.Name;
    }

    private static bool TryGetNode(JsonObject json, string name, out JsonNode? node)
    {
        if (json.TryGetPropertyValue(name, out node))
            return true;

        foreach (var property in json)
        {
            if (!string.Equals(property.Key, name, StringComparison.OrdinalIgnoreCase))
                continue;

            node = property.Value;
            return true;
        }

        node = null;
        return false;
    }
}
