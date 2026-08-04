namespace HostApp;

/// <summary>
/// Константы host-службы (совместимы с существующим ProgramData FMU-API).
/// </summary>
internal static class HostConstants
{
    public const string Manufacture = "Automation";
    public const string AppName = "FMU-API";

    /// <summary>Имя Windows-службы и корневого exe host.</summary>
    public const string ServiceName = "fmu-api";

    public const string ServiceDisplayName = "DS:FMU-API";
    public const string HostExeName = "fmu-api.exe";

    /// <summary>Первый продукт (имя папки = имя exe).</summary>
    public const string FmuProductName = "fmu-api-check";

    public const int HttpPort = 2578;

    /// <summary>Сколько последних версий продукта оставлять на диске.</summary>
    public const int VersionsToKeep = 2;
}
