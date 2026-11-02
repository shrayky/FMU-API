using FmuApiDomain.SoftwareUpdate.Models;

namespace FmuApiDomain.SoftwareUpdate;

/// Выбор самых новых пакетов клиентских приложений из файлов релиза.
public static class ClientPackageSelector
{
    public static IReadOnlyList<ReleaseAsset> SelectLatest(IEnumerable<ReleaseAsset> assets)
    {
        var selected = new Dictionary<string, (ReleaseAsset Asset, int VersionCode)>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in assets)
        {
            if (!ClientPackageName.TryParse(asset.Name, out var package))
                continue;

            var key = package.Application + "\n" + package.Abi;
            if (selected.TryGetValue(key, out var current) && current.VersionCode >= package.VersionCode)
                continue;

            selected[key] = (asset, package.VersionCode);
        }

        return selected.Values.Select(static item => item.Asset).ToArray();
    }

    public static IReadOnlyList<ReleaseAsset> SelectApplication(
        IEnumerable<ReleaseAsset> assets,
        string application)
    {
        var selected = new Dictionary<string, (ReleaseAsset Asset, int Rank, int VersionCode)>(StringComparer.OrdinalIgnoreCase);

        foreach (var asset in assets)
        {
            string abi;
            var rank = 0;
            var versionCode = 0;

            if (ClientPackageName.TryParse(asset.Name, out var package)
                && string.Equals(package.Application, application, StringComparison.OrdinalIgnoreCase))
            {
                abi = package.Abi;
                rank = 2;
                versionCode = package.VersionCode;
            }
            else if (ClientPackageName.TryParseFlutterApk(asset.Name, out abi))
            {
                rank = 1;
            }
            else
            {
                continue;
            }

            if (selected.TryGetValue(abi, out var current)
                && (current.Rank > rank || (current.Rank == rank && current.VersionCode >= versionCode)))
                continue;

            selected[abi] = (asset, rank, versionCode);
        }

        return selected.Values.Select(static item => item.Asset).ToArray();
    }
}
