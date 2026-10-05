using FmuApiDomain.SoftwareUpdate.Models;

namespace FmuApiDomain.SoftwareUpdate;

/// Выбор архива обновления из файлов релиза.
public static class SoftwarePackageSelector
{
    private const string ZipExtension = ".zip";
    private const int NamePartsCount = 4;

    public static string CurrentArchitecture() => Environment.Is64BitProcess ? "x64" : "x86";

    public static string CurrentOperatingSystem() => OperatingSystem.IsWindows() ? "win" : "linux";

    public static ReleaseAsset? SelectLatest(
        IEnumerable<ReleaseAsset> assets,
        int currentVersion,
        int currentAssembly,
        string architecture,
        string operatingSystem)
    {
        ReleaseAsset? selected = null;
        var selectedVersion = 0;
        var selectedAssembly = 0;

        foreach (var asset in assets)
        {
            if (!TryParseName(asset.Name, out var version, out var assembly, out var assetArchitecture, out var assetOs))
                continue;

            if (!string.Equals(assetArchitecture, architecture, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(assetOs, operatingSystem, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!IsNewer(version, assembly, currentVersion, currentAssembly))
                continue;

            if (selected is not null && !IsNewer(version, assembly, selectedVersion, selectedAssembly))
                continue;

            selected = asset;
            selectedVersion = version;
            selectedAssembly = assembly;
        }

        return selected;
    }

    private static bool IsNewer(int version, int assembly, int currentVersion, int currentAssembly)
        => version > currentVersion || (version == currentVersion && assembly > currentAssembly);

    /// Разбирает имя вида {версия}-{сборка}-{архитектура}-{ос}.zip.
    private static bool TryParseName(
        string name,
        out int version,
        out int assembly,
        out string architecture,
        out string operatingSystem)
    {
        version = 0;
        assembly = 0;
        architecture = string.Empty;
        operatingSystem = string.Empty;

        if (string.IsNullOrWhiteSpace(name) || !name.EndsWith(ZipExtension, StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = name[..^ZipExtension.Length].Split('-');
        if (parts.Length != NamePartsCount)
            return false;

        if (!int.TryParse(parts[0], out version) || !int.TryParse(parts[1], out assembly))
            return false;

        architecture = parts[2];
        operatingSystem = parts[3];

        return true;
    }
}
