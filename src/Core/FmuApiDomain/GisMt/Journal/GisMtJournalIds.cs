using System.Security.Cryptography;
using System.Text;

namespace FmuApiDomain.GisMt.Journal;

/// <summary>
/// Идентификаторы актов журнала ГИС МТ: акт операции Фронтола получает id из uid,
/// чтобы повторный и параллельный вызов не создали второй документ.
/// </summary>
public static class GisMtJournalIds
{
    public static string FromUid(string? uid)
    {
        var value = (uid ?? string.Empty).Trim();

        if (value.Length == 0)
            return Guid.NewGuid().ToString();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var bytes = new byte[16];

        Array.Copy(hash, bytes, bytes.Length);

        // Версия 8 по RFC 4122: идентификатор детерминированный, а не случайный.
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes).ToString();
    }
}
