namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Configuration encryption service interface
/// </summary>
public interface IConfigEncryption
{
    /// <summary>
    /// Encrypt configuration data
    /// </summary>
    /// <param name="plainText">Plaintext data</param>
    /// <returns>Encrypted data</returns>
    string Encrypt(string plainText);
    
    /// <summary>
    /// Decrypt configuration data
    /// </summary>
    /// <param name="cipherText">Encrypted data</param>
    /// <returns>Decrypted plaintext</returns>
    string Decrypt(string cipherText);
    
    /// <summary>
    /// Check whether data is already encrypted
    /// </summary>
    /// <param name="data">Data</param>
    /// <returns>Whether encrypted</returns>
    bool IsEncrypted(string data);
}
