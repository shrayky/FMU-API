using FmuApiDomain.Files;

namespace FmuApiDomain.SoftwareUpdate;

/// Файл пакета клиентского приложения в каталоге ноды.
public sealed class ClientPackageFile
{
    public ClientPackageFile(ClientPackage package, string filePath, string sha256)
    {
        Package = package;
        FilePath = filePath;
        Sha256 = sha256;
    }

    public ClientPackage Package { get; }

    public string FilePath { get; }

    public string Sha256 { get; }
}

/// Каталог скачанных пакетов клиентских приложений.
public static class ClientPackageCatalog
{
    public const string FolderName = "client-packages";

    public static string? ApplicationFolder(string application)
    {
        if (!ClientPackageName.IsApplicationId(application))
            return null;

        return Path.Combine(ServiceFolders.DataFolder(), FolderName, application);
    }

    public static IReadOnlyList<ClientPackageFile> Stored(string application)
    {
        var folder = ApplicationFolder(application);
        if (folder is null || !Directory.Exists(folder))
            return [];

        var stored = new List<ClientPackageFile>();
        foreach (var abi in new[] { ClientPackageName.Arm64, ClientPackageName.Arm32 })
        {
            var file = LatestIn(folder, application, abi);
            if (file is not null)
                stored.Add(file);
        }

        return stored;
    }

    public static ClientPackageFile? Latest(string application, string abi)
    {
        var folder = ApplicationFolder(application);
        if (folder is null || !Directory.Exists(folder))
            return null;

        return LatestIn(folder, application, abi);
    }

    public static ClientPackageFile? LatestIn(string folder, string application, string abi)
    {
        if (!Directory.Exists(folder))
            return null;

        ClientPackageFile? selected = null;

        foreach (var path in Directory.EnumerateFiles(folder, "*.apk"))
        {
            if (!ClientPackageName.TryParse(Path.GetFileName(path), out var package))
                continue;

            if (!string.Equals(package.Application, application, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(package.Abi, abi, StringComparison.OrdinalIgnoreCase))
                continue;

            if (selected is not null && package.VersionCode <= selected.Package.VersionCode)
                continue;

            var shaPath = path + ".sha256";
            if (!File.Exists(shaPath))
                continue;

            var sha256 = File.ReadAllText(shaPath).Trim();
            if (sha256.Length == 0)
                continue;

            selected = new ClientPackageFile(package, path, sha256);
        }

        return selected;
    }
}
