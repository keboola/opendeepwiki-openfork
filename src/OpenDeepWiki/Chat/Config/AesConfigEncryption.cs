using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// AES-based configuration encryption implementation
/// </summary>
public class AesConfigEncryption : IConfigEncryption
{
    private const string EncryptionPrefix = "ENC:";
    private readonly byte[] _key;
    private readonly byte[] _iv;
    
    public AesConfigEncryption(IOptions<ConfigEncryptionOptions> options)
    {
        var encryptionKey = options.Value.EncryptionKey;
        
        // If no key is configured, use default key (for development environment only)
        if (string.IsNullOrEmpty(encryptionKey))
        {
            encryptionKey = "OpenDeepWiki_Default_Key_32Bytes!";
        }
        
        // Ensure key length is 32 bytes (AES-256)
        _key = DeriveKey(encryptionKey, 32);
        // IV length is 16 bytes
        _iv = DeriveKey(encryptionKey + "_IV", 16);
    }
    
    /// <summary>
    /// Derive a byte array of specified length from a key string
    /// </summary>
    private static byte[] DeriveKey(string key, int length)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        var result = new byte[length];
        Array.Copy(hash, result, Math.Min(hash.Length, length));
        return result;
    }
    
    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;
            
        if (IsEncrypted(plainText))
            return plainText;
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return EncryptionPrefix + Convert.ToBase64String(encryptedBytes);
    }
    
    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;
            
        if (!IsEncrypted(cipherText))
            return cipherText;
        
        var encryptedData = cipherText[EncryptionPrefix.Length..];
        var encryptedBytes = Convert.FromBase64String(encryptedData);
        
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        
        return Encoding.UTF8.GetString(decryptedBytes);
    }
    
    /// <inheritdoc />
    public bool IsEncrypted(string data)
    {
        return !string.IsNullOrEmpty(data) && data.StartsWith(EncryptionPrefix);
    }
}

/// <summary>
/// Configuration encryption options
/// </summary>
public class ConfigEncryptionOptions
{
    /// <summary>
    /// Encryption key
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;
}
