using FmuApiDomain.SoftwareUpdate;
using FmuApiDomain.SoftwareUpdate.Models;
using Xunit;

namespace FmuApiApplication.Tests;

public class ClientPackageTests
{
    [Fact]
    public void Имя_пакета_разбирается_на_приложение_версию_и_abi()
    {
        var parsed = ClientPackageName.TryParse("tsd-1.0.3-4-arm64-v8a.apk", out var package);

        Assert.True(parsed);
        Assert.Equal("tsd", package.Application);
        Assert.Equal("1.0.3", package.VersionName);
        Assert.Equal(4, package.VersionCode);
        Assert.Equal(ClientPackageName.Arm64, package.Abi);
        Assert.Equal("tsd-1.0.3-4-arm64-v8a.apk", package.FileName);
    }

    [Theory]
    [InlineData("app-arm64-v8a-release.apk", "arm64-v8a")]
    [InlineData("app-armeabi-v7a-release.apk", "armeabi-v7a")]
    public void Имя_сборки_flutter_разбирается_на_abi(string fileName, string abi)
    {
        var parsed = ClientPackageName.TryParseFlutterApk(fileName, out var parsedAbi);

        Assert.True(parsed);
        Assert.Equal(abi, parsedAbi);
    }

    [Fact]
    public void Версия_читается_из_собранного_apk()
    {
        var apk = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "Presentation", "TsdApp", "build", "app", "outputs", "flutter-apk", "app-arm64-v8a-release.apk"));
        if (!File.Exists(apk))
            return;

        var read = ApkVersion.TryRead(apk, out var versionName, out var versionCode);

        Assert.True(read);
        Assert.Equal("1.0.2", versionName);
        Assert.Equal(2003, versionCode);
    }

    [Fact]
    public void Имя_пакета_32_бит_разбирается()
    {
        var parsed = ClientPackageName.TryParse("tsd-1.0.2-3-armeabi-v7a.apk", out var package);

        Assert.True(parsed);
        Assert.Equal(ClientPackageName.Arm32, package.Abi);
        Assert.Equal(3, package.VersionCode);
    }

    [Theory]
    [InlineData("12-3-x64-win.zip")]
    [InlineData("tsd-1.0.3-4-x64.apk")]
    [InlineData("tsd-1.0.3-arm64-v8a.apk")]
    [InlineData("../tsd-1.0.0-1-arm64-v8a.apk")]
    public void Чужое_или_битое_имя_не_считается_пакетом(string fileName)
    {
        Assert.False(ClientPackageName.TryParse(fileName, out _));
    }

    [Fact]
    public void Из_релиза_берётся_старший_пакет_каждого_приложения_и_abi()
    {
        var assets = new List<ReleaseAsset>
        {
            Asset("12-3-x64-win.zip"),
            Asset("tsd-1.0.2-3-arm64-v8a.apk"),
            Asset("tsd-1.0.3-4-arm64-v8a.apk"),
            Asset("tsd-1.0.3-10-armeabi-v7a.apk"),
            Asset("tsd-1.0.4-5-armeabi-v7a.apk"),
        };

        var selected = ClientPackageSelector.SelectLatest(assets).Select(asset => asset.Name).ToHashSet();

        Assert.Equal(2, selected.Count);
        Assert.Contains("tsd-1.0.3-4-arm64-v8a.apk", selected);
        Assert.Contains("tsd-1.0.3-10-armeabi-v7a.apk", selected);
    }

    [Fact]
    public void Каталог_отдаёт_старший_пакет_с_контрольной_суммой()
    {
        var folder = Path.Combine(Path.GetTempPath(), "fmu-client-packages-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        try
        {
            WritePackage(folder, "tsd-1.0.2-3-arm64-v8a.apk", "aaa");
            WritePackage(folder, "tsd-1.0.3-4-arm64-v8a.apk", "bbb");
            File.WriteAllText(Path.Combine(folder, "tsd-1.0.9-9-arm64-v8a.apk"), "no-sum");

            var latest = ClientPackageCatalog.LatestIn(folder, "tsd", ClientPackageName.Arm64);

            Assert.NotNull(latest);
            Assert.Equal(4, latest.Package.VersionCode);
            Assert.Equal("bbb", latest.Sha256);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Theory]
    [InlineData("sha256:ABC", "abc", true)]
    [InlineData("sha256:abc", "abd", false)]
    [InlineData("", "abc", false)]
    [InlineData("md5:abc", "abc", false)]
    public void Digest_релиза_сравнивается_с_sha256_файла(string digest, string sha256, bool expected)
    {
        Assert.Equal(expected, ReleaseDigest.Matches(digest, sha256));
    }

    private static ReleaseAsset Asset(string name) => new()
    {
        Name = name,
        BrowserDownloadUrl = "https://example.com/" + name
    };

    private static void WritePackage(string folder, string fileName, string sha256)
    {
        var path = Path.Combine(folder, fileName);
        File.WriteAllText(path, "apk");
        File.WriteAllText(path + ".sha256", sha256);
    }
}
