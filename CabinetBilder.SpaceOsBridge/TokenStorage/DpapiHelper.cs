using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace CabinetBilder.SpaceOsBridge.TokenStorage;

/// <summary>
/// Helper for Windows Data Protection API (DPAPI).
/// Provides field-level and file-level encryption.
/// </summary>
public static class DpapiHelper
{
    private static readonly byte[] Entropy = "CabinetBilder_Entropy_2024"u8.ToArray();

    public static string EncryptString(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;
        
        var data = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = EncryptBytes(data);
        return Convert.ToBase64String(encrypted);
    }

    public static string DecryptString(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return string.Empty;

        var data = Convert.FromBase64String(ciphertext);
        var decrypted = DecryptBytes(data);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static byte[] EncryptBytes(byte[] data)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Fallback for non-windows (Dev/CI only)
            return data;
        }

        return ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
    }

    public static byte[] DecryptBytes(byte[] encryptedData)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Fallback for non-windows (Dev/CI only)
            return encryptedData;
        }

        return ProtectedData.Unprotect(encryptedData, Entropy, DataProtectionScope.CurrentUser);
    }
}
