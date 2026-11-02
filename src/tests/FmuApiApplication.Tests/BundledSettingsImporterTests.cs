using ApplicationConfigurationService;
using FmuApiDomain.Configuration;
using Xunit;

namespace FmuApiApplication.Tests;

public class BundledSettingsImporterTests
{
    [Fact]
    public void Первый_запуск_берёт_settings_json()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "settings.json"), """
                { "serverConfig": { "apiIpPort": 2599 } }
                """);
            File.WriteAllText(Path.Combine(dir.FullName, "config.json"), """
                { "serverConfig": { "apiIpPort": 1111 } }
                """);
            var settings = new Parameters();

            var changed = BundledSettingsImporter.TryImport(
                settings, dir.FullName, Path.Combine(dir.FullName, "program-data", "config.json"), out var error);

            Assert.True(changed);
            Assert.Null(error);
            Assert.Equal(2599, settings.ServerConfig.ApiIpPort);
            Assert.True(File.Exists(Path.Combine(dir.FullName, "settings.json")));
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Нет_файла_ничего_не_меняет()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var settings = new Parameters();

            var changed = BundledSettingsImporter.TryImport(
                settings, dir.FullName, Path.Combine(dir.FullName, "program-data", "config.json"), out var error);

            Assert.False(changed);
            Assert.Null(error);
            Assert.Equal(2578, settings.ServerConfig.ApiIpPort);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Битый_json_не_меняет_настройки()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            File.WriteAllText(Path.Combine(dir.FullName, "settings.json"), "{");
            var settings = new Parameters();

            var changed = BundledSettingsImporter.TryImport(
                settings, dir.FullName, Path.Combine(dir.FullName, "program-data", "config.json"), out var error);

            Assert.False(changed);
            Assert.False(string.IsNullOrWhiteSpace(error));
            Assert.Equal(2578, settings.ServerConfig.ApiIpPort);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [Fact]
    public void Уже_есть_config_в_ProgramData_импорт_не_нужен()
    {
        Assert.False(BundledSettingsImporter.NeedsBundledImport(true));
        Assert.True(BundledSettingsImporter.NeedsBundledImport(false));
    }
}
