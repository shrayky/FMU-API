namespace HostApp.Models;

internal sealed record ProductInfo(
    string Name,
    string RootDirectory,
    IReadOnlyList<ProductVersionInfo> Versions)
{
    public IReadOnlyList<ProductVersionInfo> OrderedDescending =>
        Versions.OrderByDescending(v => v.Version).ToList();

    public ProductVersionInfo? Latest => OrderedDescending.FirstOrDefault();
}
