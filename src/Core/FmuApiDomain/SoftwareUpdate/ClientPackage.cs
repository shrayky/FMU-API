namespace FmuApiDomain.SoftwareUpdate;

/// Пакет клиентского приложения в релизе: {приложение}-{versionName}-{versionCode}-{abi}.apk.
public readonly record struct ClientPackage(string Application, string VersionName, int VersionCode, string Abi)
{
    public string FileName => $"{Application}-{VersionName}-{VersionCode}-{Abi}.apk";
}

/// Разбор имени пакета клиентского приложения.
public static class ClientPackageName
{
    public const string Arm64 = "arm64-v8a";
    public const string Arm32 = "armeabi-v7a";

    private const string ApkExtension = ".apk";

    public static bool IsApplicationId(string application) =>
        application.Length > 0
        && application.All(static character => char.IsAsciiLetterOrDigit(character));

    public static bool TryParse(string fileName, out ClientPackage package)
    {
        package = default;

        if (string.IsNullOrWhiteSpace(fileName)
            || !fileName.EndsWith(ApkExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        var stem = fileName[..^ApkExtension.Length];
        if (!TrySplitAbi(stem, out var head, out var abi))
            return false;

        var versionSeparator = head.LastIndexOf('-');
        if (versionSeparator <= 0 || versionSeparator == head.Length - 1)
            return false;

        if (!int.TryParse(head[(versionSeparator + 1)..], out var versionCode) || versionCode < 0)
            return false;

        var prefix = head[..versionSeparator];
        var applicationSeparator = prefix.IndexOf('-');
        if (applicationSeparator <= 0 || applicationSeparator == prefix.Length - 1)
            return false;

        var application = prefix[..applicationSeparator];
        var versionName = prefix[(applicationSeparator + 1)..];
        if (!IsApplicationId(application) || !IsVersionName(versionName))
            return false;

        package = new ClientPackage(application, versionName, versionCode, abi);
        return true;
    }

    public static bool TryParseFlutterApk(string fileName, out string abi)
    {
        abi = string.Empty;
        const string prefix = "app-";
        const string suffix = "-release.apk";
        if (string.IsNullOrWhiteSpace(fileName)
            || !fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return false;

        var name = fileName[prefix.Length..^suffix.Length];
        if (name.Equals(Arm64, StringComparison.OrdinalIgnoreCase))
        {
            abi = Arm64;
            return true;
        }

        if (name.Equals(Arm32, StringComparison.OrdinalIgnoreCase))
        {
            abi = Arm32;
            return true;
        }

        return false;
    }

    private static bool TrySplitAbi(string stem, out string head, out string abi)
    {
        head = string.Empty;
        abi = string.Empty;

        if (EndsWithAbi(stem, Arm64, out head))
        {
            abi = Arm64;
            return true;
        }

        if (EndsWithAbi(stem, Arm32, out head))
        {
            abi = Arm32;
            return true;
        }

        return false;
    }

    private static bool EndsWithAbi(string stem, string abi, out string head)
    {
        var suffix = "-" + abi;
        if (!stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            head = string.Empty;
            return false;
        }

        head = stem[..^suffix.Length];
        return true;
    }

    private static bool IsVersionName(string versionName)
    {
        if (versionName.Length == 0 || versionName[0] == '.' || versionName[^1] == '.')
            return false;

        var dotSeen = false;
        foreach (var character in versionName)
        {
            if (character == '.')
            {
                if (dotSeen)
                    return false;

                dotSeen = true;
                continue;
            }

            if (!char.IsAsciiDigit(character))
                return false;

            dotSeen = false;
        }

        return true;
    }
}
