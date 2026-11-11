using System.IO.Compression;
using System.Security.Cryptography;
using CSharpFunctionalExtensions;

namespace FmuApiDomain.SoftwareUpdate;

/// Кладёт выбранный APK в каталог пакетов ноды.
public static class ClientPackageStore
{
    public const long MaxPackageBytes = 80L * 1024 * 1024;

    public static Result<ClientPackage> Accept(string folder, string sourcePath, string fileName, string application)
    {
        if (!ClientPackageName.IsApplicationId(application))
            return Result.Failure<ClientPackage>("Некорректное имя приложения");

        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)
            || !safeName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<ClientPackage>("Нужен файл APK");

        var info = new FileInfo(sourcePath);
        if (!info.Exists || info.Length == 0)
            return Result.Failure<ClientPackage>("Файл APK пустой");

        if (info.Length > MaxPackageBytes)
            return Result.Failure<ClientPackage>("APK больше 80 МБ");

        if (!TryAbi(sourcePath, safeName, application, out var abi, out var abiError))
            return Result.Failure<ClientPackage>(abiError);

        if (!ApkVersion.TryRead(sourcePath, out var versionName, out var versionCode))
            return Result.Failure<ClientPackage>("Не удалось прочитать версию APK");

        var package = new ClientPackage(application, versionName, versionCode, abi);
        var sha256 = ComputeSha256(sourcePath);
        Directory.CreateDirectory(folder);

        var destination = Path.Combine(folder, package.FileName);
        if (!SamePath(sourcePath, destination))
            File.Move(sourcePath, destination, overwrite: true);

        File.WriteAllText(destination + ".sha256", sha256);
        DeleteOthers(folder, package, destination);
        return Result.Success(package);
    }

    public static void DeleteOthers(string folder, ClientPackage kept, string keptPath)
    {
        if (!Directory.Exists(folder))
            return;

        foreach (var path in Directory.EnumerateFiles(folder, "*.apk"))
        {
            if (SamePath(path, keptPath))
                continue;

            if (!ClientPackageName.TryParse(Path.GetFileName(path), out var package))
                continue;

            if (!string.Equals(package.Application, kept.Application, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(package.Abi, kept.Abi, StringComparison.OrdinalIgnoreCase))
                continue;

            File.Delete(path);

            var shaPath = path + ".sha256";
            if (File.Exists(shaPath))
                File.Delete(shaPath);
        }
    }

    private static bool TryAbi(
        string apkPath,
        string fileName,
        string application,
        out string abi,
        out string error)
    {
        if (TryFileAbi(fileName, application, out abi))
        {
            error = string.Empty;
            return true;
        }

        if (!TryLibraryAbi(apkPath, out abi, out var several, out var unreadable))
        {
            error = unreadable
                ? "Не удалось прочитать APK"
                : several
                    ? "В APK несколько архитектур"
                    : "Не удалось определить архитектуру APK";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryFileAbi(string fileName, string application, out string abi)
    {
        if (ClientPackageName.TryParse(fileName, out var package)
            && string.Equals(package.Application, application, StringComparison.OrdinalIgnoreCase))
        {
            abi = package.Abi;
            return true;
        }

        return ClientPackageName.TryParseFlutterApk(fileName, out abi);
    }

    private static bool TryLibraryAbi(string apkPath, out string abi, out bool several, out bool unreadable)
    {
        abi = string.Empty;
        several = false;
        unreadable = false;

        try
        {
            using var archive = ZipFile.OpenRead(apkPath);
            string? found = null;
            foreach (var entry in archive.Entries)
            {
                if (!TryEntryAbi(entry.FullName, out var entryAbi))
                    continue;

                if (found is null)
                {
                    found = entryAbi;
                    continue;
                }

                if (!string.Equals(found, entryAbi, StringComparison.OrdinalIgnoreCase))
                {
                    several = true;
                    return false;
                }
            }

            if (found is null)
                return false;

            abi = found;
            return true;
        }
        catch (Exception)
        {
            unreadable = true;
            return false;
        }
    }

    private static bool TryEntryAbi(string entryName, out string abi)
    {
        abi = string.Empty;
        var name = entryName.Replace('\\', '/');
        const string prefix = "lib/";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var rest = name[prefix.Length..];
        var slash = rest.IndexOf('/');
        if (slash <= 0)
            return false;

        var folder = rest[..slash];
        if (folder.Equals(ClientPackageName.Arm64, StringComparison.OrdinalIgnoreCase))
        {
            abi = ClientPackageName.Arm64;
            return true;
        }

        if (folder.Equals(ClientPackageName.Arm32, StringComparison.OrdinalIgnoreCase))
        {
            abi = ClientPackageName.Arm32;
            return true;
        }

        return false;
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}
