using System.Security.Cryptography;
using System.Text;

namespace OpenDeepWiki.Chat.Providers.WeChat;

/// <summary>
/// WeChat message encryption/decryption utility class
/// Implements the WeChat Official Platform message encryption/decryption scheme
/// </summary>
public class WeChatCrypto
{
    private readonly string _token;
    private readonly string _appId;
    private readonly byte[] _aesKey;
    private readonly byte[] _iv;
    
    /// <summary>
    /// Initialize WeChat encryption/decryption utility
    /// </summary>
    /// <param name="token">Token configured in WeChat server settings</param>
    /// <param name="encodingAesKey">Message encryption/decryption key (43 characters)</param>
    /// <param name="appId">WeChat AppID</param>
    public WeChatCrypto(string token, string encodingAesKey, string appId)
    {
        _token = token;
        _appId = appId;
        
        // EncodingAESKey is a Base64-encoded AES key (43 chars + "=" = 44-char Base64)
        _aesKey = Convert.FromBase64String(encodingAesKey + "=");
        // IV is the first 16 bytes of the AES key
        _iv = _aesKey[..16];
    }
    
    /// <summary>
    /// Verify message signature
    /// </summary>
    /// <param name="signature">WeChat encrypted signature</param>
    /// <param name="timestamp">Timestamp</param>
    /// <param name="nonce">Nonce</param>
    /// <param name="encrypt">Encrypted message body (optional, for message encryption mode)</param>
    /// <returns>Whether the signature is valid</returns>
    public bool VerifySignature(string signature, string timestamp, string nonce, string? encrypt = null)
    {
        var calculatedSignature = CalculateSignature(timestamp, nonce, encrypt);
        return string.Equals(signature, calculatedSignature, StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Calculate message signature
    /// </summary>
    /// <param name="timestamp">Timestamp</param>
    /// <param name="nonce">Nonce</param>
    /// <param name="encrypt">Encrypted message body (optional)</param>
    /// <returns>Signature string</returns>
    public string CalculateSignature(string timestamp, string nonce, string? encrypt = null)
    {
        var items = encrypt != null 
            ? new[] { _token, timestamp, nonce, encrypt }
            : new[] { _token, timestamp, nonce };
        
        // Lexicographic sort
        Array.Sort(items, StringComparer.Ordinal);
        
        // Concatenate and compute SHA1 hash
        var combined = string.Concat(items);
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(combined));
        
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    
    /// <summary>
    /// Decrypt message
    /// </summary>
    /// <param name="encryptedContent">Encrypted message content (Base64 encoded)</param>
    /// <returns>Decrypted message content, or null if decryption fails</returns>
    public string? Decrypt(string encryptedContent)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedContent);
            
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            
            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            // Parse the decrypted data
            // Format: random(16 bytes) + msg_len(4 bytes) + msg + appid
            
            // Skip the first 16 bytes of random data
            var msgLenBytes = decryptedBytes[16..20];
            // Convert network byte order (big-endian) to message length
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(msgLenBytes);
            }
            var msgLen = BitConverter.ToInt32(msgLenBytes, 0);
            
            // Extract message content
            var msgBytes = decryptedBytes[20..(20 + msgLen)];
            var message = Encoding.UTF8.GetString(msgBytes);
            
            // Extract and verify AppID
            var appIdStart = 20 + msgLen;
            var appIdBytes = RemovePkcs7Padding(decryptedBytes[appIdStart..]);
            var appId = Encoding.UTF8.GetString(appIdBytes);
            
            if (appId != _appId)
            {
                return null; // AppID mismatch
            }
            
            return message;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Encrypt message
    /// </summary>
    /// <param name="message">Message content to encrypt</param>
    /// <returns>Encrypted message (Base64 encoded)</returns>
    public string Encrypt(string message)
    {
        // Generate 16 bytes of random data
        var random = new byte[16];
        RandomNumberGenerator.Fill(random);
        
        // Message content bytes
        var msgBytes = Encoding.UTF8.GetBytes(message);
        
        // Message length (4 bytes, network byte order)
        var msgLenBytes = BitConverter.GetBytes(msgBytes.Length);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(msgLenBytes);
        }
        
        // AppID bytes
        var appIdBytes = Encoding.UTF8.GetBytes(_appId);
        
        // Assemble plaintext: random(16) + msg_len(4) + msg + appid
        var plainBytes = new byte[random.Length + msgLenBytes.Length + msgBytes.Length + appIdBytes.Length];
        var offset = 0;
        
        Buffer.BlockCopy(random, 0, plainBytes, offset, random.Length);
        offset += random.Length;
        
        Buffer.BlockCopy(msgLenBytes, 0, plainBytes, offset, msgLenBytes.Length);
        offset += msgLenBytes.Length;
        
        Buffer.BlockCopy(msgBytes, 0, plainBytes, offset, msgBytes.Length);
        offset += msgBytes.Length;
        
        Buffer.BlockCopy(appIdBytes, 0, plainBytes, offset, appIdBytes.Length);
        
        // PKCS7 padding
        var paddedBytes = AddPkcs7Padding(plainBytes);
        
        // AES encryption
        using var aes = Aes.Create();
        aes.Key = _aesKey;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        
        using var encryptor = aes.CreateEncryptor();
        var encryptedBytes = encryptor.TransformFinalBlock(paddedBytes, 0, paddedBytes.Length);
        
        return Convert.ToBase64String(encryptedBytes);
    }
    
    /// <summary>
    /// Generate XML response for encrypted message
    /// </summary>
    /// <param name="encryptedContent">Encrypted message content</param>
    /// <param name="timestamp">Timestamp</param>
    /// <param name="nonce">Nonce</param>
    /// <returns>XML formatted encrypted response</returns>
    public string GenerateEncryptedXml(string encryptedContent, string timestamp, string nonce)
    {
        var signature = CalculateSignature(timestamp, nonce, encryptedContent);
        
        return $@"<xml>
<Encrypt><![CDATA[{encryptedContent}]]></Encrypt>
<MsgSignature><![CDATA[{signature}]]></MsgSignature>
<TimeStamp>{timestamp}</TimeStamp>
<Nonce><![CDATA[{nonce}]]></Nonce>
</xml>";
    }
    
    /// <summary>
    /// Add PKCS7 padding
    /// </summary>
    private static byte[] AddPkcs7Padding(byte[] data)
    {
        const int blockSize = 32; // WeChat uses 32-byte block size
        var paddingLength = blockSize - (data.Length % blockSize);
        
        var paddedData = new byte[data.Length + paddingLength];
        Buffer.BlockCopy(data, 0, paddedData, 0, data.Length);
        
        for (var i = data.Length; i < paddedData.Length; i++)
        {
            paddedData[i] = (byte)paddingLength;
        }
        
        return paddedData;
    }
    
    /// <summary>
    /// Remove PKCS7 padding
    /// </summary>
    private static byte[] RemovePkcs7Padding(byte[] data)
    {
        if (data.Length == 0)
            return data;
        
        var paddingLength = data[^1];
        if (paddingLength > data.Length || paddingLength > 32)
            return data;
        
        return data[..^paddingLength];
    }
}
