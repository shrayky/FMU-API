using CentralServerExchange.Services;
using CSharpFunctionalExtensions;
using FmuApiDomain.CentralServiceExchange.Models;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FmuApiApplication.Tests;

public class SidecarConnectionImporterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "fmu-sidecar-" + Guid.NewGuid());
    private readonly SidecarConnectionImporter _importer = new(NullLogger<SidecarConnectionImporter>.Instance);

    public SidecarConnectionImporterTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void TryImport_заполняет_пустые_настройки_и_включает_обмен()
    {
        WriteSidecar("""
            {
              "Address": "http://localhost:2590",
              "Token": "e880fc66-93f7-43e4-b48a-3ad98016e99a"
            }
            """);

        var settings = new Parameters();

        var imported = _importer.TryImport(settings, _tempDir);

        Assert.True(imported);
        Assert.Equal("http://localhost:2590", settings.FmuApiCentralServer.Address);
        Assert.Equal("e880fc66-93f7-43e4-b48a-3ad98016e99a", settings.FmuApiCentralServer.Token);
        Assert.True(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_читает_serverAddress_из_дистрибутива()
    {
        WriteSidecar("""
            {
              "serverAddress": "http://localhost:2580",
              "token": "70eeae41-2664-4d76-adc0-584b5576563b"
            }
            """);

        var settings = new Parameters();

        var imported = _importer.TryImport(settings, _tempDir);

        Assert.True(imported);
        Assert.Equal("http://localhost:2580", settings.FmuApiCentralServer.Address);
        Assert.Equal("70eeae41-2664-4d76-adc0-584b5576563b", settings.FmuApiCentralServer.Token);
        Assert.True(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_не_перезаписывает_заполненные_настройки()
    {
        WriteSidecar("""
            {
              "Address": "http://localhost:2590",
              "Token": "e880fc66-93f7-43e4-b48a-3ad98016e99a"
            }
            """);

        var settings = new Parameters();
        settings.FmuApiCentralServer.Address = "http://already-set";
        settings.FmuApiCentralServer.Token = "already-set-token";

        var imported = _importer.TryImport(settings, _tempDir);

        Assert.False(imported);
        Assert.Equal("http://already-set", settings.FmuApiCentralServer.Address);
        Assert.Equal("already-set-token", settings.FmuApiCentralServer.Token);
        Assert.False(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_пропускает_если_заполнен_только_адрес()
    {
        WriteSidecar("""
            {
              "Address": "http://localhost:2590",
              "Token": "e880fc66-93f7-43e4-b48a-3ad98016e99a"
            }
            """);

        var settings = new Parameters();
        settings.FmuApiCentralServer.Address = "http://already-set";

        var imported = _importer.TryImport(settings, _tempDir);

        Assert.False(imported);
        Assert.Equal(string.Empty, settings.FmuApiCentralServer.Token);
        Assert.False(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_пропускает_если_файла_нет()
    {
        var settings = new Parameters();

        var imported = _importer.TryImport(settings, _tempDir);

        Assert.False(imported);
        Assert.Equal(string.Empty, settings.FmuApiCentralServer.Address);
        Assert.Equal(string.Empty, settings.FmuApiCentralServer.Token);
        Assert.False(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_пропускает_битый_json()
    {
        WriteSidecar("{ not-json");

        var settings = new Parameters();

        var imported = _importer.TryImport(settings, _tempDir);

        Assert.False(imported);
        Assert.Equal(string.Empty, settings.FmuApiCentralServer.Address);
        Assert.Equal(string.Empty, settings.FmuApiCentralServer.Token);
        Assert.False(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_пропускает_пустые_значения_в_файле()
    {
        WriteSidecar("""
            {
              "Address": "",
              "Token": ""
            }
            """);

        var settings = new Parameters();

        var imported = _importer.TryImport(settings, _tempDir);

        Assert.False(imported);
        Assert.False(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_читает_config_рядом_с_host_exe()
    {
        var versionDir = Path.Combine(_tempDir, "fmu-api-check", "12.2");
        Directory.CreateDirectory(versionDir);
        File.WriteAllText(Path.Combine(_tempDir, "fmu-api.exe"), string.Empty);
        WriteSidecar("""
            {
              "Address": "http://localhost:2590",
              "Token": "e880fc66-93f7-43e4-b48a-3ad98016e99a"
            }
            """);

        var settings = new Parameters();

        var imported = _importer.TryImport(settings, versionDir);

        Assert.True(imported);
        Assert.Equal("http://localhost:2590", settings.FmuApiCentralServer.Address);
        Assert.Equal("e880fc66-93f7-43e4-b48a-3ad98016e99a", settings.FmuApiCentralServer.Token);
        Assert.True(settings.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public void TryImport_не_берёт_config_из_родителя_без_host_exe()
    {
        var versionDir = Path.Combine(_tempDir, "fmu-api-check", "12.2");
        Directory.CreateDirectory(versionDir);
        WriteSidecar("""
            {
              "Address": "http://localhost:2590",
              "Token": "e880fc66-93f7-43e4-b48a-3ad98016e99a"
            }
            """);

        var settings = new Parameters();

        var imported = _importer.TryImport(settings, versionDir);

        Assert.False(imported);
        Assert.Equal(string.Empty, settings.FmuApiCentralServer.Address);
    }

    [Fact]
    public void TryImport_не_удаляет_файл()
    {
        WriteSidecar("""
            {
              "Address": "http://localhost:2590",
              "Token": "e880fc66-93f7-43e4-b48a-3ad98016e99a"
            }
            """);

        _importer.TryImport(new Parameters(), _tempDir);

        Assert.True(File.Exists(SidecarPath()));
    }

    [Fact]
    public async Task ApplyIfNeededAsync_сохраняет_настройки_после_импорта()
    {
        WriteSidecar("""
            {
              "Address": "http://localhost:2590",
              "Token": "e880fc66-93f7-43e4-b48a-3ad98016e99a"
            }
            """);

        var parameters = new FakeParametersService();

        await _importer.ApplyIfNeededAsync(parameters, _tempDir);

        Assert.Equal(1, parameters.UpdateCount);
        Assert.Equal("http://localhost:2590", parameters.Value.FmuApiCentralServer.Address);
        Assert.True(parameters.Value.FmuApiCentralServer.Enabled);
    }

    [Fact]
    public async Task ApplyIfNeededAsync_не_сохраняет_если_импорта_не_было()
    {
        var parameters = new FakeParametersService();

        await _importer.ApplyIfNeededAsync(parameters, _tempDir);

        Assert.Equal(0, parameters.UpdateCount);
    }

    private void WriteSidecar(string json)
    {
        File.WriteAllText(SidecarPath(), json);
    }

    private string SidecarPath() => Path.Combine(_tempDir, "config.json");

    private sealed class FakeParametersService : IParametersService
    {
        public Parameters Value { get; set; } = new();
        public int UpdateCount { get; private set; }

        public Task<Parameters> CurrentAsync() => Task.FromResult(Value);

        public Parameters Current() => Value;

        public Task UpdateAsync(Parameters parameters)
        {
            Value = parameters;
            UpdateCount++;
            return Task.CompletedTask;
        }

        public Task<Result> ApplyFromCentral(FmuApiSetting value) => Task.FromResult(Result.Success());
    }
}
