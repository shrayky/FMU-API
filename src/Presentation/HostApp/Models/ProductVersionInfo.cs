namespace HostApp.Models;

/// <summary>
/// Одна установленная версия продукта.
/// </summary>
internal sealed record ProductVersionInfo(
    Version Version,
    string DirectoryPath,
    string ExecutablePath);
