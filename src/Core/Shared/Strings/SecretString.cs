using System.Security.Cryptography;
using System.Text;

namespace Shared.Strings;

public static class SecretString
{
    public static string EncryptData(string data, string secret)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));

        // Создаем случайный IV
        var iv = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(iv);

        // Шифруем данные
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var encryptedData = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

        // Объединяем IV и зашифрованные данные
        var result = new byte[iv.Length + encryptedData.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encryptedData, 0, result, iv.Length, encryptedData.Length);

        // Кодируем в Base64
        return Convert.ToBase64String(result);
    }

    public static string DecryptData(string encryptedData, string secret)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedData);

        // Извлекаем IV (первые 16 байт)
        var iv = new byte[16];
        Buffer.BlockCopy(encryptedBytes, 0, iv, 0, 16);

        // Извлекаем зашифрованные данные
        var cipherText = new byte[encryptedBytes.Length - 16];
        Buffer.BlockCopy(encryptedBytes, 16, cipherText, 0, cipherText.Length);

        // Создаем ключ
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));

        // Расшифровываем
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}