namespace FmuApiDomain.SoftwareUpdate;

/// Файл APK, выбранный для записи в каталог пакетов.
public sealed class ClientPackageUpload
{
    public ClientPackageUpload(string fileName, Stream content, long length)
    {
        FileName = fileName;
        Content = content;
        Length = length;
    }

    public string FileName { get; }

    public Stream Content { get; }

    public long Length { get; }
}
