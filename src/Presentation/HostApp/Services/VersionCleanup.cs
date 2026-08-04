using HostApp.Models;

namespace HostApp.Services;

internal sealed class VersionCleanup(ILogger<VersionCleanup> logger)
{
    public void Cleanup(ProductInfo product, IReadOnlySet<string> protectedPaths, int versionsToKeep)
    {
        if (versionsToKeep < 1)
            versionsToKeep = 1;

        var ordered = product.OrderedDescending;
        if (ordered.Count <= versionsToKeep)
            return;

        foreach (var obsolete in ordered.Skip(versionsToKeep))
        {
            if (IsProtected(obsolete.DirectoryPath, protectedPaths))
            {
                logger.LogInformation(
                    "Версия {Version} продукта {Product} защищена от удаления (запущена)",
                    obsolete.Version,
                    product.Name);
                continue;
            }

            try
            {
                Directory.Delete(obsolete.DirectoryPath, recursive: true);
                logger.LogInformation(
                    "Удалена старая версия {Version} продукта {Product}",
                    obsolete.Version,
                    product.Name);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Не удалось удалить {Path} продукта {Product}",
                    obsolete.DirectoryPath,
                    product.Name);
            }
        }
    }

    private static bool IsProtected(string directoryPath, IReadOnlySet<string> protectedPaths)
    {
        var normalized = Normalize(directoryPath);
        return protectedPaths.Any(p =>
            string.Equals(Normalize(p), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
