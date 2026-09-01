using FmuApiDomain.Attributes;
using FmuApiDomain.Connectivity.Interfaces;
using FmuApiDomain.Connectivity.Models;
using FmuApiDomain.Constants;
using Microsoft.Extensions.DependencyInjection;
using Shared.FilesFolders;
using Shared.Json;
using System.Text.Json;

namespace FmuApiApplication.Connectivity.Services;

[AutoRegisterService(ServiceLifetime.Singleton)]
public class CrptEspHostFileStore : ICrptEspHostStore
{
    private const string FileName = "crpt-esp-hosts.json";
    private readonly string _path = Path.Combine(
        Folders.CommonApplicationDataFolder(ApplicationInformation.Manufacture, ApplicationInformation.AppName),
        FileName);
    private readonly SemaphoreSlim _lock = new(1, 1);

    public IReadOnlyList<CrptEspHost> Load()
    {
        _lock.Wait();
        try
        {
            var hosts = ReadFromFile();
            if (hosts.Count > 0)
                return hosts;

            SaveDefaults();
            return CrptEspHosts.Defaults;
        }
        catch (Exception)
        {
            return CrptEspHosts.Defaults;
        }
        finally
        {
            _lock.Release();
        }
    }

    private IReadOnlyList<CrptEspHost> ReadFromFile()
    {
        if (!File.Exists(_path) || new FileInfo(_path).Length == 0)
            return [];

        var json = File.ReadAllText(_path);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        var hosts = JsonSerializer.Deserialize<List<CrptEspHost>>(json, JsonSerializeOptionsProvider.Default());
        return hosts?
            .Where(host => !string.IsNullOrWhiteSpace(host.Url))
            .ToList() ?? [];
    }

    private void SaveDefaults()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(CrptEspHosts.Defaults, JsonSerializeOptionsProvider.Default());
        File.WriteAllText(_path, json);
    }
}
