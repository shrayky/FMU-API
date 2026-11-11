using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using FmuApiDomain.SoftwareUpdate;
using Xunit;

namespace FmuApiApplication.Tests;

public class ClientPackageStoreTests
{
    [Fact]
    public void Загрузка_apk_заменяет_пакет_той_же_архитектуры_даже_старшей_сборкой()
    {
        var folder = NewFolder();
        try
        {
            WriteStored(folder, "tsd-9.0.0-99-arm64-v8a.apk", "old-arm64");
            WriteStored(folder, "tsd-1.0.0-1-armeabi-v7a.apk", "keep-arm32");
            var source = Path.Combine(folder, "app-arm64-v8a-release.apk");
            WriteApk(source, "1.0.1", 2);

            var stored = ClientPackageStore.Accept(folder, source, "app-arm64-v8a-release.apk", "tsd");

            Assert.True(stored.IsSuccess);
            Assert.Equal("tsd-1.0.1-2-arm64-v8a.apk", stored.Value.FileName);
            Assert.False(File.Exists(Path.Combine(folder, "tsd-9.0.0-99-arm64-v8a.apk")));
            Assert.False(File.Exists(Path.Combine(folder, "tsd-9.0.0-99-arm64-v8a.apk.sha256")));
            Assert.True(File.Exists(Path.Combine(folder, "tsd-1.0.0-1-armeabi-v7a.apk")));

            var destination = Path.Combine(folder, stored.Value.FileName);
            var sha256 = File.ReadAllText(destination + ".sha256").Trim();
            Assert.Equal(Sha256(destination), sha256);

            var latest = ClientPackageCatalog.LatestIn(folder, "tsd", ClientPackageName.Arm64);
            Assert.NotNull(latest);
            Assert.Equal(2, latest.Package.VersionCode);
            Assert.Equal(sha256, latest.Sha256);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Архитектура_без_имени_берётся_из_каталога_lib()
    {
        var folder = NewFolder();
        try
        {
            var source = Path.Combine(folder, "local.apk");
            WriteApk(source, "1.0.4", 8, "lib/armeabi-v7a/libapp.so");

            var stored = ClientPackageStore.Accept(folder, source, "local.apk", "tsd");

            Assert.True(stored.IsSuccess);
            Assert.Equal(ClientPackageName.Arm32, stored.Value.Abi);
            Assert.Equal("tsd-1.0.4-8-armeabi-v7a.apk", stored.Value.FileName);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Apk_с_двумя_архитектурами_без_имени_отклоняется()
    {
        var folder = NewFolder();
        try
        {
            var source = Path.Combine(folder, "universal.apk");
            WriteApk(
                source,
                "1.0.4",
                8,
                "lib/arm64-v8a/libapp.so",
                "lib/armeabi-v7a/libapp.so");

            var stored = ClientPackageStore.Accept(folder, source, "universal.apk", "tsd");

            Assert.True(stored.IsFailure);
            Assert.Equal("В APK несколько архитектур", stored.Error);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public void Apk_без_версии_отклоняется()
    {
        var folder = NewFolder();
        try
        {
            var source = Path.Combine(folder, "app-arm64-v8a-release.apk");
            WriteApk(source, versionName: "", versionCode: 0);

            var stored = ClientPackageStore.Accept(folder, source, "app-arm64-v8a-release.apk", "tsd");

            Assert.True(stored.IsFailure);
            Assert.Equal("Не удалось прочитать версию APK", stored.Error);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    private static string NewFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "fmu-client-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void WriteStored(string folder, string fileName, string sha256)
    {
        var path = Path.Combine(folder, fileName);
        File.WriteAllText(path, "apk");
        File.WriteAllText(path + ".sha256", sha256);
    }

    private static void WriteApk(string path, string versionName, int versionCode, params string[] entries)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        var manifest = archive.CreateEntry("AndroidManifest.xml");
        using (var stream = manifest.Open())
        {
            var bytes = BinaryManifest(versionName, versionCode);
            stream.Write(bytes, 0, bytes.Length);
        }

        foreach (var entryName in entries)
        {
            var entry = archive.CreateEntry(entryName);
            using var stream = entry.Open();
            stream.WriteByte(0);
        }
    }

    private static byte[] BinaryManifest(string versionName, int versionCode)
    {
        var name = versionName.Length == 0 ? " " : versionName;
        var pool = StringPool("versionCode", "versionName", name);
        var element = StartElement(versionCode, versionName.Length == 0 ? -1 : 2);
        var manifest = new byte[8 + pool.Length + element.Length];
        manifest[2] = 8;
        pool.CopyTo(manifest, 8);
        element.CopyTo(manifest, 8 + pool.Length);
        return manifest;
    }

    private static byte[] StringPool(params string[] values)
    {
        var blobs = new byte[values.Length][];
        var offsets = new int[values.Length];
        var cursor = 0;
        for (var index = 0; index < values.Length; index++)
        {
            offsets[index] = cursor;
            var chars = Encoding.Unicode.GetBytes(values[index]);
            blobs[index] = new byte[2 + chars.Length];
            BitConverter.GetBytes((ushort)values[index].Length).CopyTo(blobs[index], 0);
            chars.CopyTo(blobs[index], 2);
            cursor += blobs[index].Length;
        }

        const int headerSize = 28;
        var stringsStart = headerSize + values.Length * 4;
        var pool = new byte[stringsStart + cursor];
        WriteU16(pool, 0, 0x0001);
        WriteU16(pool, 2, headerSize);
        WriteU32(pool, 4, (uint)pool.Length);
        WriteU32(pool, 8, (uint)values.Length);
        WriteU32(pool, 20, (uint)stringsStart);
        for (var index = 0; index < values.Length; index++)
            WriteU32(pool, headerSize + index * 4, (uint)offsets[index]);

        cursor = stringsStart;
        foreach (var blob in blobs)
        {
            blob.CopyTo(pool, cursor);
            cursor += blob.Length;
        }

        return pool;
    }

    private static byte[] StartElement(int versionCode, int versionNameIndex)
    {
        const int headerSize = 16;
        const int attributeStart = 16;
        const int attributeSize = 20;
        const int attributeCount = 2;
        var chunk = new byte[headerSize + attributeStart + attributeCount * attributeSize];
        WriteU16(chunk, 0, 0x0102);
        WriteU16(chunk, 2, headerSize);
        WriteU32(chunk, 4, (uint)chunk.Length);
        WriteU16(chunk, headerSize + 8, attributeStart);
        WriteU16(chunk, headerSize + 10, attributeSize);
        WriteU16(chunk, headerSize + 12, attributeCount);
        WriteAttribute(chunk, headerSize + attributeStart, 0, 0x10, (uint)versionCode);
        if (versionNameIndex >= 0)
            WriteAttribute(chunk, headerSize + attributeStart + attributeSize, 1, 0x03, (uint)versionNameIndex);
        return chunk;
    }

    private static void WriteAttribute(byte[] chunk, int offset, int nameIndex, byte dataType, uint data)
    {
        WriteU32(chunk, offset + 4, (uint)nameIndex);
        WriteU32(chunk, offset + 8, 0xFFFFFFFF);
        chunk[offset + 15] = dataType;
        WriteU32(chunk, offset + 16, data);
    }

    private static void WriteU16(byte[] data, int offset, ushort value) =>
        BitConverter.GetBytes(value).CopyTo(data, offset);

    private static void WriteU32(byte[] data, int offset, uint value) =>
        BitConverter.GetBytes(value).CopyTo(data, offset);

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
