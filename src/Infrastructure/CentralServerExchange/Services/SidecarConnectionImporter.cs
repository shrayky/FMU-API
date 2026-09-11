using CentralServerExchange.Models;
using FmuApiDomain.Attributes;
using FmuApiDomain.Configuration;
using FmuApiDomain.Configuration.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Json;
using System.Text.Json;

namespace CentralServerExchange.Services;

/// <summary>
/// Заполняет пустые настройки обмена с central из config.json рядом с exe.
/// </summary>
[AutoRegisterService(ServiceLifetime.Singleton)]
public class SidecarConnectionImporter
{
    private readonly ILogger<SidecarConnectionImporter> _logger;

    public SidecarConnectionImporter(ILogger<SidecarConnectionImporter> logger)
    {
        _logger = logger;
    }

    public bool TryImport(Parameters settings, string? executableDirectory = null)
    {
        if (!ConnectionIsEmpty(settings))
            return false;

        var startDirectory = ResolveStartDirectory(executableDirectory);
        var sidecarPath = FindSidecarPath(startDirectory);

        if (sidecarPath is null)
        {
            _logger.LogInformation(
                "Sidecar config.json не найден. Старт поиска: {Dir}",
                startDirectory);
            return false;
        }

        try
        {
            var json = File.ReadAllText(sidecarPath);
            var sidecar = JsonSerializer.Deserialize<SidecarCentralConnection>(
                json,
                JsonSerializeOptionsProvider.Default());

            var address = sidecar?.ResolvedAddress() ?? string.Empty;

            if (sidecar == null ||
                string.IsNullOrWhiteSpace(address) ||
                string.IsNullOrWhiteSpace(sidecar.Token))
            {
                _logger.LogWarning("Файл {Path} не содержит Address и Token", sidecarPath);
                return false;
            }

            settings.FmuApiCentralServer.Address = address;
            settings.FmuApiCentralServer.Token = sidecar.Token;
            settings.FmuApiCentralServer.Enabled = true;

            _logger.LogInformation("Настройки обмена с central загружены из {Path}", sidecarPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось прочитать {Path}", sidecarPath);
            return false;
        }
    }

    public async Task ApplyIfNeededAsync(IParametersService parameters, string? executableDirectory = null)
    {
        var settings = await parameters.CurrentAsync().ConfigureAwait(false);

        if (!TryImport(settings, executableDirectory))
            return;

        await parameters.UpdateAsync(settings).ConfigureAwait(false);
    }

    private static bool ConnectionIsEmpty(Parameters settings)
    {
        return string.IsNullOrWhiteSpace(settings.FmuApiCentralServer.Address)
            && string.IsNullOrWhiteSpace(settings.FmuApiCentralServer.Token);
    }

    private static string ResolveStartDirectory(string? executableDirectory)
    {
        if (!string.IsNullOrWhiteSpace(executableDirectory))
            return executableDirectory;

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath))
        {
            var processDir = Path.GetDirectoryName(processPath);
            if (!string.IsNullOrWhiteSpace(processDir))
                return processDir;
        }

        return AppContext.BaseDirectory;
    }

    private static string? FindSidecarPath(string startDirectory)
    {
        var localSidecar = Path.Combine(startDirectory, SidecarFileName);
        if (File.Exists(localSidecar))
            return localSidecar;

        var current = startDirectory;
        for (var i = 0; i < MaxParentLevels; i++)
        {
            var parent = Directory.GetParent(current)?.FullName;
            if (string.IsNullOrWhiteSpace(parent) || parent == current)
                return null;

            if (DirectoryContainsHostExe(parent))
            {
                var hostSidecar = Path.Combine(parent, SidecarFileName);
                return File.Exists(hostSidecar) ? hostSidecar : null;
            }

            current = parent;
        }

        return null;
    }

    private static bool DirectoryContainsHostExe(string directory)
    {
        return File.Exists(Path.Combine(directory, HostExeWindows))
            || File.Exists(Path.Combine(directory, HostExeUnix));
    }

    private const string SidecarFileName = "config.json";
    private const string HostExeWindows = "fmu-api.exe";
    private const string HostExeUnix = "fmu-api";
    private const int MaxParentLevels = 3;
}
