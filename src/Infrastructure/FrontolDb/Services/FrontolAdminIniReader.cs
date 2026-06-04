using FmuApiDomain.Configuration.Options;

namespace FrontolDb.Services;

public class FrontolAdminIniReader
{
    private const string DefaultIniPath = @"C:\ProgramData\ATOL\Frontol6\Settings\FrontolAdmin.ini";

    public (bool Success, string? Error, List<FrontolConnectionSettings> Connections) Read(string? iniPath = null)
    {
        var path = string.IsNullOrWhiteSpace(iniPath) ? DefaultIniPath : iniPath;

        if (!File.Exists(path))
            return (false, $"Файл не найден: {path}", []);

        var sections = ParseSections(File.ReadAllLines(path));
        var connections = new List<FrontolConnectionSettings>();

        var id = 1;

        foreach (var (sectionName, values) in sections)
        {
            if (!sectionName.StartsWith("DB", StringComparison.OrdinalIgnoreCase))
                continue;

            var pathBase = GetValue(values, "Path");
            var dbFile = GetValue(values, "DB");
            var user = GetValue(values, "User", "SYSDBA");
            var password = string.Equals(user, "SYSDBA", StringComparison.OrdinalIgnoreCase)
                ? "masterkey"
                : string.Empty;

            connections.Add(new FrontolConnectionSettings
            {
                Id = id,
                Name = GetValue(values, "Name"),
                Path = BuildDatabasePath(pathBase, dbFile),
                UserName = user,
                Password = password
            });

            id++;
        }

        return (true, null, connections);
    }

    private static string BuildDatabasePath(string pathBase, string dbFile)
    {
        if (string.IsNullOrWhiteSpace(pathBase))
            return dbFile;

        if (string.IsNullOrWhiteSpace(dbFile))
            return pathBase.TrimEnd('\\', '/');

        var directory = pathBase.TrimEnd('\\', '/');
        return $"{directory}\\{dbFile}";
    }

    private static string GetValue(Dictionary<string, string> values, string key, string defaultValue = "")
    {
        return values.TryGetValue(key, out var value) ? value : defaultValue;
    }

    private static Dictionary<string, Dictionary<string, string>> ParseSections(IEnumerable<string> lines)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string>? current = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var sectionName = line[1..^1].Trim();
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                sections[sectionName] = current;
                continue;
            }

            if (current == null)
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            current[key] = value;
        }

        return sections;
    }
}
