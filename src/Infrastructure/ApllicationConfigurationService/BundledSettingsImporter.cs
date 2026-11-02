using FmuApiDomain.Configuration;

namespace ApplicationConfigurationService;

public static class BundledSettingsImporter
{
    public const string FileName = "settings.json";

    public static bool NeedsBundledImport(bool programDataConfigExists) => !programDataConfigExists;

    public static bool TryImport(
        Parameters settings,
        string executableDirectory,
        string programDataConfigPath,
        out string? error)
    {
        error = null;
        var path = Path.Combine(executableDirectory, FileName);
        if (!File.Exists(path))
            return false;

        if (string.Equals(Path.GetFullPath(path), Path.GetFullPath(programDataConfigPath), StringComparison.OrdinalIgnoreCase))
            return false;

        var json = File.ReadAllText(path);
        if (!SettingsExchange.TryApply(settings, json, out var changed, out error))
            return false;

        return changed;
    }
}
