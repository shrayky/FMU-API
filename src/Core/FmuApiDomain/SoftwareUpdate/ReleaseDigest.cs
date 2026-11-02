namespace FmuApiDomain.SoftwareUpdate;

/// Сравнение digest файла релиза GitHub с SHA-256 скачанного пакета.
public static class ReleaseDigest
{
    public const string Sha256Prefix = "sha256:";

    public static bool Matches(string digest, string sha256)
    {
        if (string.IsNullOrWhiteSpace(digest)
            || string.IsNullOrWhiteSpace(sha256)
            || !digest.StartsWith(Sha256Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        return digest[Sha256Prefix.Length..].Equals(sha256, StringComparison.OrdinalIgnoreCase);
    }
}
