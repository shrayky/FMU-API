using FmuApiDomain.SoftwareUpdate;
using FmuApiDomain.SoftwareUpdate.Models;
using Xunit;

namespace FmuApiApplication.Tests;

public class SoftwarePackageSelectorTests
{
    private const int CurrentVersion = 12;
    private const int CurrentAssembly = 2;

    [Fact]
    public void Выбирается_архив_новее_текущей_сборки_этой_ос_и_архитектуры()
    {
        var assets = Assets(
            "12-2-x64-win.zip",
            "12-1-x64-win.zip",
            "12-3-x64-win.zip",
            "12-3-x86-win.zip",
            "12-3-x64-linux.zip");

        var selected = Select(assets);

        Assert.Equal("12-3-x64-win.zip", selected?.Name);
    }

    [Fact]
    public void Архив_старшей_версии_новее_текущей_сборки()
    {
        var assets = Assets("12-2-x64-win.zip", "13-1-x64-win.zip");

        var selected = Select(assets);

        Assert.Equal("13-1-x64-win.zip", selected?.Name);
    }

    [Fact]
    public void Из_нескольких_подходящих_архивов_берётся_старшая_пара_версии_и_сборки()
    {
        var assets = Assets("12-3-x64-win.zip", "13-1-x64-win.zip", "12-10-x64-win.zip", "13-2-x64-win.zip");

        var selected = Select(assets);

        Assert.Equal("13-2-x64-win.zip", selected?.Name);
    }

    [Fact]
    public void Сборка_сравнивается_как_число_а_не_как_строка()
    {
        var assets = Assets("12-3-x64-win.zip", "12-10-x64-win.zip");

        var selected = Select(assets);

        Assert.Equal("12-10-x64-win.zip", selected?.Name);
    }

    [Fact]
    public void Без_подходящего_архива_кандидат_не_выбирается()
    {
        var assets = Assets("12-2-x64-win.zip", "12-1-x64-win.zip", "12-9-x86-win.zip", "13-1-x64-linux.zip", "release-notes.txt");

        var selected = Select(assets);

        Assert.Null(selected);
    }

    private static ReleaseAsset? Select(IReadOnlyList<ReleaseAsset> assets) =>
        SoftwarePackageSelector.SelectLatest(assets, CurrentVersion, CurrentAssembly, "x64", "win");

    private static List<ReleaseAsset> Assets(params string[] names) =>
        names
            .Select(name => new ReleaseAsset
            {
                Name = name,
                BrowserDownloadUrl = $"https://example.com/{name}"
            })
            .ToList();
}
