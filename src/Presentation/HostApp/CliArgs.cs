namespace HostApp;

/// <summary>
/// Разбор аргументов командной строки.
/// </summary>
internal static class CliArgs
{
    /// <summary>
    /// Возвращает значение аргумента после ключа, либо defaultValue.
    /// </summary>
    public static string Value(string[] args, string key, string defaultValue = "")
    {
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i].Trim();

            if (a.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                return a[(key.Length + 1)..].Trim();

            if (string.Equals(a, key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                return args[i + 1].Trim();
        }

        return defaultValue;
    }

    /// <summary>
    /// Проверяет наличие флага.
    /// </summary>
    public static bool Has(string[] args, string key) =>
        args.Any(a => string.Equals(a.Trim(), key, StringComparison.OrdinalIgnoreCase));
}
