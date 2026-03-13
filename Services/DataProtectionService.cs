using System.Security.Cryptography;
using System.Text;

namespace HBS.Services;

public class DataProtectionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public DataProtectionService()
    {
        // In production, load from secure configuration (Azure Key Vault, AWS Secrets Manager, etc.)
        _key = Encoding.UTF8.GetBytes("MySecureKey12345MySecureKey12345"); // 32 bytes for AES-256
        _iv = Encoding.UTF8.GetBytes("MySecureIV123456"); // 16 bytes
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }

    public string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@')) return email;
        
        var parts = email.Split('@');
        var username = parts[0];
        var masked = username.Length > 2 
            ? $"{username[0]}***{username[^1]}@{parts[1]}" 
            : $"***@{parts[1]}";
        return masked;
    }

    public string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4) return "***";
        return $"***-{phone[^4..]}";
    }
}
