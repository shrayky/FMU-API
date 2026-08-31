using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main()
    {
        EnsureNukeRootDirectory();
        return Execute<Build>(x => x.Release);
    }

    const string ProductName = "fmu-api-check";
    const string HostExeName = "fmu-api.exe";
    const string LinuxBinaryName = "fmu-api";

    [Parameter("Версия архива, например 12-1")]
    readonly string Version = default!;

    [PathVariable("npm")]
    readonly Tool Npm = default!;

    AbsolutePath WebApiProject => RootDirectory / "src" / "Presentation" / "WebApi" / "WebApi.csproj";
    AbsolutePath HostAppProject => RootDirectory / "src" / "Presentation" / "HostApp" / "HostApp.csproj";
    AbsolutePath WwwrootDirectory => RootDirectory / "src" / "Presentation" / "WebApi" / "wwwroot";
    AbsolutePath BuildsDirectory => RootDirectory / "builds";

    AbsolutePath ProductWinX64 => BuildsDirectory / "x64 full";
    AbsolutePath ProductWinX86 => BuildsDirectory / "x86 full";
    AbsolutePath ProductLinuxX64 => BuildsDirectory / "x64 linux";
    AbsolutePath HostWinX64 => BuildsDirectory / "ha-win-x64";
    AbsolutePath HostWinX86 => BuildsDirectory / "ha-win-x86";

    string? _archiveVersion;
    string? _versionFolder;

    Target BuildFrontend => _ => _
        .Executes(() =>
        {
            Npm("ci", workingDirectory: WwwrootDirectory);
            Npm("run build", workingDirectory: WwwrootDirectory);
        });

    Target PublishWeb => _ => _
        .DependsOn(BuildFrontend)
        .Executes(() =>
        {
            PublishProduct("win-x64", ProductWinX64);
            PublishProduct("win-x86", ProductWinX86);
            PublishProduct("linux-x64", ProductLinuxX64);
        });

    Target PublishHost => _ => _
        .Executes(() =>
        {
            PublishHostApp("win-x64", HostWinX64);
            PublishHostApp("win-x86", HostWinX86);
        });

    Target ResolveVersion => _ => _
        .Unlisted()
        .Executes(() => EnsureArchiveVersion());

    Target PackWindows => _ => _
        .DependsOn(PublishWeb, PublishHost, ResolveVersion)
        .Executes(() =>
        {
            PackWindowsPlatform("x64", HostWinX64, ProductWinX64);
            PackWindowsPlatform("x86", HostWinX86, ProductWinX86);
        });

    Target PackLinux => _ => _
        .DependsOn(PublishWeb, ResolveVersion)
        .Executes(() =>
        {
            var productBinary = ProductLinuxX64 / ProductName;
            var wwwroot = ProductLinuxX64 / "wwwroot";
            Assert.True(productBinary.FileExists(), $"Не найден продукт: {productBinary}");
            Assert.True(wwwroot.DirectoryExists(), $"Не найден wwwroot: {wwwroot}");

            var staging = CreateCleanStaging("linux-x64");
            CopyItem(productBinary, staging / LinuxBinaryName);
            CopyItem(wwwroot, staging / "wwwroot");

            ZipStaging(staging, BuildsDirectory / $"{EnsureArchiveVersion()}-x64-linux.zip");
        });

    Target Release => _ => _
        .DependsOn(PackWindows, PackLinux)
        .Executes(() =>
        {
            var version = EnsureArchiveVersion();
            Serilog.Log.Information("Archives successfully created:");
            Serilog.Log.Information("- {Zip}", BuildsDirectory / $"{version}-x64-win.zip");
            Serilog.Log.Information("- {Zip}", BuildsDirectory / $"{version}-x86-win.zip");
            Serilog.Log.Information("- {Zip}", BuildsDirectory / $"{version}-x64-linux.zip");
        });

    /// <summary>
    /// Публикует WebApi в self-contained single-file; vite уже собран в BuildFrontend.
    /// </summary>
    void PublishProduct(string runtime, AbsolutePath output)
    {
        output.CreateOrCleanDirectory();
        DotNetPublish(s => s
            .SetProject(WebApiProject)
            .SetConfiguration("Release")
            .SetRuntime(runtime)
            .SetSelfContained(true)
            .SetPublishSingleFile(true)
            .SetOutput(output)
            .SetProperty("SkipNpmBuild", "true"));
    }

    /// <summary>
    /// Публикует host-приложение для Windows.
    /// </summary>
    void PublishHostApp(string runtime, AbsolutePath output)
    {
        output.CreateOrCleanDirectory();
        DotNetPublish(s => s
            .SetProject(HostAppProject)
            .SetConfiguration("Release")
            .SetRuntime(runtime)
            .SetSelfContained(true)
            .SetPublishSingleFile(true)
            .SetOutput(output));
    }

    /// <summary>
    /// Собирает zip: fmu-api.exe + fmu-api-check/{версия}/.
    /// </summary>
    void PackWindowsPlatform(string arch, AbsolutePath hostDir, AbsolutePath productDir)
    {
        var hostExe = hostDir / HostExeName;
        var productExe = productDir / $"{ProductName}.exe";
        var wwwroot = productDir / "wwwroot";

        Assert.True(hostExe.FileExists(), $"Не найден host: {hostExe}");
        Assert.True(productExe.FileExists(), $"Не найден продукт: {productExe}");
        Assert.True(wwwroot.DirectoryExists(), $"Не найден wwwroot: {wwwroot}");

        var staging = CreateCleanStaging($"win-{arch}");
        var versionDir = staging / ProductName / EnsureVersionFolder();
        versionDir.CreateDirectory();

        CopyItem(hostExe, staging / HostExeName);
        CopyItem(productExe, versionDir / $"{ProductName}.exe");
        CopyItem(wwwroot, versionDir / "wwwroot");

        ZipStaging(staging, BuildsDirectory / $"{EnsureArchiveVersion()}-{arch}-win.zip");
    }

    /// <summary>
    /// Возвращает версию архива; если не передана --Version, спрашивает в консоли.
    /// </summary>
    string EnsureArchiveVersion()
    {
        if (!string.IsNullOrWhiteSpace(_archiveVersion))
            return _archiveVersion;

        var value = string.IsNullOrWhiteSpace(Version)
            ? ReadVersionFromConsole()
            : Version;

        Assert.False(string.IsNullOrWhiteSpace(value), "Sorry, wrong version number.");
        _archiveVersion = value;
        _versionFolder = value.Replace("-", ".", StringComparison.Ordinal);
        return value;
    }

    /// <summary>
    /// Каталог версии для хоста: 12-1 -> 12.1, чтобы сработал Version.TryParse.
    /// </summary>
    string EnsureVersionFolder()
    {
        EnsureArchiveVersion();
        return _versionFolder!;
    }

    /// <summary>
    /// Спрашивает версию в консоли, как старые make*.cmd.
    /// </summary>
    static string ReadVersionFromConsole()
    {
        Console.Write("Print software version (for example 12-1): ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Создаёт пустой каталог раскладки архива.
    /// </summary>
    AbsolutePath CreateCleanStaging(string suffix)
    {
        var staging = TemporaryDirectory / $"fmu-archive-{suffix}";
        staging.CreateOrCleanDirectory();
        return staging;
    }

    /// <summary>
    /// Упаковывает содержимое staging в zip и удаляет временный каталог.
    /// </summary>
    void ZipStaging(AbsolutePath staging, AbsolutePath zip)
    {
        BuildsDirectory.CreateDirectory();
        if (zip.FileExists())
            zip.DeleteFile();
        staging.ZipTo(zip);
        staging.DeleteDirectory();
        Serilog.Log.Information("Создан {Zip}", zip);
    }

    /// <summary>
    /// Копирует файл или каталог в назначение.
    /// </summary>
    static void CopyItem(AbsolutePath source, AbsolutePath destination)
    {
        if (source.FileExists())
        {
            destination.Parent.CreateDirectory();
            File.Copy(source, destination, overwrite: true);
            return;
        }

        Assert.True(source.DirectoryExists(), $"Не найден каталог: {source}");
        destination.CreateDirectory();

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = destination / relative;
            target.Parent.CreateDirectory();
            File.Copy(file, target, overwrite: true);
        }
    }

    /// <summary>
    /// Ставит текущий каталог на корень репозитория (маркер .nuke), если процесс запущен не из него.
    /// </summary>
    static void EnsureNukeRootDirectory()
    {
        if (TryFindNukeRoot(Directory.GetCurrentDirectory(), out _))
            return;

        if (TryFindNukeRoot(AppContext.BaseDirectory, out var root))
            Directory.SetCurrentDirectory(root);
    }

    /// <summary>
    /// Ищет каталог с маркером .nuke, поднимаясь от start вверх по дереву.
    /// </summary>
    static bool TryFindNukeRoot(string start, out string root)
    {
        var current = new DirectoryInfo(Path.GetFullPath(start));
        while (current != null)
        {
            var marker = Path.Combine(current.FullName, ".nuke");
            if (Directory.Exists(marker) || File.Exists(marker))
            {
                root = current.FullName;
                return true;
            }

            current = current.Parent;
        }

        root = string.Empty;
        return false;
    }
}
