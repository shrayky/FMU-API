using System.Security.Cryptography;
using System.Text;

namespace Shared.Strings;

public static class InstanceHmac
{
    public static string Sign(string secret, string token, long unixTimestamp, string nonce)
    {
        if (string.IsNullOrEmpty(secret))
            throw new ArgumentException("Секретный ключ не задан", nameof(secret));

        var payload = $"{token}\n{unixTimestamp}\n{nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    public static bool Verify(string secret, string token, long unixTimestamp, string nonce, string signature)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature))
            return false;

        try
        {
            var expected = Convert.FromHexString(Sign(secret, token, unixTimestamp, nonce));
            var actual = Convert.FromHexString(signature.Trim());
            return expected.Length == actual.Length
                   && CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
