using System.IO.Compression;
using System.Text;

namespace FmuApiDomain.SoftwareUpdate;

/// Версия из AndroidManifest.xml внутри APK.
public static class ApkVersion
{
    public static bool TryRead(string apkPath, out string versionName, out int versionCode)
    {
        versionName = string.Empty;
        versionCode = 0;

        try
        {
            using var archive = ZipFile.OpenRead(apkPath);
            var entry = archive.GetEntry("AndroidManifest.xml");
            if (entry is null)
                return false;

            using var stream = entry.Open();
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return TryReadManifest(memory.ToArray(), out versionName, out versionCode);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool TryReadManifest(byte[] manifest, out string versionName, out int versionCode)
    {
        versionName = string.Empty;
        versionCode = 0;
        if (manifest.Length < 8)
            return false;

        var strings = ReadStringPool(manifest, ReadU16(manifest, 2));
        if (strings.Count == 0)
            return false;

        var offset = 8;
        while (offset + 8 <= manifest.Length)
        {
            var type = ReadU16(manifest, offset);
            var chunkSize = ReadU32(manifest, offset + 4);
            if (chunkSize < 8 || offset + chunkSize > manifest.Length)
                break;

            if (type == 0x0102)
                ReadElement(manifest, offset, strings, ref versionName, ref versionCode);

            offset += (int)chunkSize;
        }

        return versionCode > 0 && versionName.Length > 0;
    }

    private static void ReadElement(
        byte[] manifest,
        int chunkStart,
        IReadOnlyList<string> strings,
        ref string versionName,
        ref int versionCode)
    {
        var headerSize = ReadU16(manifest, chunkStart + 2);
        if (headerSize < 16 || chunkStart + headerSize + 14 > manifest.Length)
            return;

        var extension = chunkStart + headerSize;
        var attributeStart = ReadU16(manifest, extension + 8);
        var attributeSize = ReadU16(manifest, extension + 10);
        var attributeCount = ReadU16(manifest, extension + 12);
        if (attributeSize < 20 || attributeCount == 0)
            return;

        for (var index = 0; index < attributeCount; index++)
        {
            var attribute = extension + attributeStart + index * attributeSize;
            if (attribute + 20 > manifest.Length)
                return;

            var name = StringAt(strings, ReadU32(manifest, attribute + 4));
            var rawValue = ReadU32(manifest, attribute + 8);
            var dataType = manifest[attribute + 15];
            var data = ReadU32(manifest, attribute + 16);

            if (name == "versionCode" && dataType is 0x10 or 0x11)
                versionCode = (int)data;
            else if (name == "versionName")
                versionName = dataType == 0x03 ? StringAt(strings, data) : StringAt(strings, rawValue);
        }
    }

    private static List<string> ReadStringPool(byte[] manifest, int poolOffset)
    {
        var strings = new List<string>();
        if (poolOffset < 0 || poolOffset + 28 > manifest.Length || ReadU16(manifest, poolOffset) != 0x0001)
            return strings;

        var stringCount = (int)ReadU32(manifest, poolOffset + 8);
        var flags = ReadU32(manifest, poolOffset + 16);
        var stringsStart = poolOffset + (int)ReadU32(manifest, poolOffset + 20);
        var utf8 = (flags & 0x100) != 0;
        if (stringCount <= 0 || stringsStart > manifest.Length)
            return strings;

        for (var index = 0; index < stringCount; index++)
        {
            var offsetPosition = poolOffset + 28 + index * 4;
            if (offsetPosition + 4 > manifest.Length)
                break;

            var start = stringsStart + (int)ReadU32(manifest, offsetPosition);
            strings.Add(utf8 ? ReadUtf8(manifest, start) : ReadUtf16(manifest, start));
        }

        return strings;
    }

    private static string ReadUtf16(byte[] manifest, int start)
    {
        if (start + 2 > manifest.Length)
            return string.Empty;

        var length = (int)ReadU16(manifest, start);
        start += 2;
        if ((length & 0x8000) != 0)
        {
            if (start + 2 > manifest.Length)
                return string.Empty;

            length = ((length & 0x7FFF) << 16) | ReadU16(manifest, start);
            start += 2;
        }

        if (length < 0 || start + length * 2 > manifest.Length)
            return string.Empty;

        return Encoding.Unicode.GetString(manifest, start, length * 2);
    }

    private static string ReadUtf8(byte[] manifest, int start)
    {
        if (!SkipUtf8Length(manifest, ref start) || !TryUtf8Length(manifest, ref start, out var byteLength))
            return string.Empty;

        if (start + byteLength > manifest.Length)
            return string.Empty;

        return Encoding.UTF8.GetString(manifest, start, byteLength);
    }

    private static bool SkipUtf8Length(byte[] manifest, ref int start) => TryUtf8Length(manifest, ref start, out _);

    private static bool TryUtf8Length(byte[] manifest, ref int start, out int length)
    {
        length = 0;
        if (start >= manifest.Length)
            return false;

        var first = manifest[start++];
        if ((first & 0x80) == 0)
        {
            length = first;
            return true;
        }

        if (start >= manifest.Length)
            return false;

        length = ((first & 0x7F) << 8) | manifest[start++];
        return true;
    }

    private static string StringAt(IReadOnlyList<string> strings, uint index)
    {
        if (index == 0xFFFFFFFF || index >= strings.Count)
            return string.Empty;

        return strings[(int)index];
    }

    private static ushort ReadU16(byte[] data, int offset) => BitConverter.ToUInt16(data, offset);

    private static uint ReadU32(byte[] data, int offset) => BitConverter.ToUInt32(data, offset);
}
