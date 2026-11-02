namespace FmuApiDomain.SoftwareUpdate;

/// Адрес репозитория GitHub в URL метаданных последнего релиза.
public static class GitHubReleaseAddress
{
    private const string WebPrefix = "https://github.com/";
    private const string ApiPrefix = "https://api.github.com/repos/";

    public static bool TryMetadataUrl(string? address, out string metadataUrl)
    {
        metadataUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(address))
            return false;

        var text = address.Trim().TrimEnd('/');
        string path;

        if (text.StartsWith(ApiPrefix, StringComparison.OrdinalIgnoreCase))
            path = text[ApiPrefix.Length..];
        else if (text.StartsWith(WebPrefix, StringComparison.OrdinalIgnoreCase))
            path = text[WebPrefix.Length..];
        else if (text.Contains("://", StringComparison.Ordinal))
            return false;
        else
            path = text;

        if (!TryOwnerRepo(path, out var owner, out var repo))
            return false;

        metadataUrl = $"{ApiPrefix}{owner}/{repo}/releases/latest";
        return true;
    }

    private static bool TryOwnerRepo(string path, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        if (parts.Length == 3 && !parts[2].Equals("releases", StringComparison.OrdinalIgnoreCase))
            return false;

        if (parts.Length == 4
            && (!parts[2].Equals("releases", StringComparison.OrdinalIgnoreCase)
                || !parts[3].Equals("latest", StringComparison.OrdinalIgnoreCase)))
            return false;

        if (parts.Length > 4 || !IsName(parts[0]) || !IsName(parts[1]))
            return false;

        owner = parts[0];
        repo = parts[1];
        return true;
    }

    private static bool IsName(string value)
    {
        if (value.Length is 0 or > 100 || value[0] == '.' || value[^1] == '.')
            return false;

        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
                continue;

            return false;
        }

        return true;
    }
}
