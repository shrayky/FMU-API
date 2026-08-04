using HostApp.Models;

namespace HostApp.Services;

internal sealed class ProductDiscovery(ILogger<ProductDiscovery> logger)
{
    public IReadOnlyList<ProductInfo> Discover(string installRoot)
    {
        if (!Directory.Exists(installRoot))
        {
            logger.LogWarning("Каталог установки не найден: {Root}", installRoot);
            return [];
        }

        var products = new List<ProductInfo>();

        foreach (var productDir in Directory.EnumerateDirectories(installRoot))
        {
            var productName = Path.GetFileName(productDir);
            if (string.IsNullOrWhiteSpace(productName))
                continue;

            var versions = DiscoverVersions(productName, productDir);
            if (versions.Count == 0)
                continue;

            products.Add(new ProductInfo(productName, productDir, versions));
            logger.LogInformation(
                "Найден продукт {Product}: версий {Count}, старшая {Latest}",
                productName,
                versions.Count,
                versions.Max(v => v.Version));
        }

        return products;
    }

    private List<ProductVersionInfo> DiscoverVersions(string productName, string productDir)
    {
        var expectedExe = $"{productName}.exe";
        var versions = new List<ProductVersionInfo>();

        foreach (var versionDir in Directory.EnumerateDirectories(productDir))
        {
            var versionName = Path.GetFileName(versionDir);
            if (!Version.TryParse(versionName, out var version))
            {
                logger.LogDebug(
                    "Пропуск каталога {Dir}: имя не является версией",
                    versionDir);
                continue;
            }

            var exePath = Path.Combine(versionDir, expectedExe);
            if (!File.Exists(exePath))
            {
                logger.LogWarning(
                    "Пропуск версии {Version} продукта {Product}: нет файла {Exe}",
                    versionName,
                    productName,
                    expectedExe);
                continue;
            }

            versions.Add(new ProductVersionInfo(version, versionDir, exePath));
        }

        return versions;
    }
}
