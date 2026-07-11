using System.Runtime.InteropServices;
using CabinetBilder.SpaceOsBridge.TokenStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetBilder.Tests.Auth;

[TestClass]
public class DpapiHelperTests
{
    [TestMethod]
    public void EncryptDecrypt_String_RoundTrip()
    {
        // Only run on Windows as ProtectedData is Windows-only for actual encryption
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("DPAPI test requires Windows.");
            return;
        }

        // Arrange
        string original = "SensitiveData_123!@#";

        // Act
        string encrypted = DpapiHelper.EncryptString(original);
        string decrypted = DpapiHelper.DecryptString(encrypted);

        // Assert
        Assert.AreNotEqual(original, encrypted);
        Assert.AreEqual(original, decrypted);
    }

    [TestMethod]
    public void EncryptDecrypt_Bytes_RoundTrip()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Assert.Inconclusive("DPAPI test requires Windows.");
            return;
        }

        // Arrange
        byte[] original = { 1, 2, 3, 4, 5, 255 };

        // Act
        byte[] encrypted = DpapiHelper.EncryptBytes(original);
        byte[] decrypted = DpapiHelper.DecryptBytes(encrypted);

        // Assert
        CollectionAssert.AreNotEqual(original, encrypted);
        CollectionAssert.AreEqual(original, decrypted);
    }
}
