namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Идентификатор документа из ответа lk/documents/create: тело ответа — JSON-строка.
/// </summary>
public static class GisMtDocumentId
{
    public static string Parse(string? body)
    {
        var value = (body ?? string.Empty).Trim();

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            value = value[1..^1].Trim();

        return value;
    }
}
