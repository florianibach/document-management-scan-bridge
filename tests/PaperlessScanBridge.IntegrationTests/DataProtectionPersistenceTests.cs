using Microsoft.AspNetCore.DataProtection;

namespace PaperlessScanBridge.IntegrationTests;

public sealed class DataProtectionPersistenceTests
{
    [Fact]
    public void SeparateProvidersCanDecryptWithPersistedKeyRing()
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        try
        {
            var first = DataProtectionProvider.Create(directory, options => options.SetApplicationName("PaperlessScanBridge"));
            var protectedValue = first.CreateProtector("antiforgery-test").Protect("token");
            var restarted = DataProtectionProvider.Create(directory, options => options.SetApplicationName("PaperlessScanBridge"));
            Assert.Equal("token", restarted.CreateProtector("antiforgery-test").Unprotect(protectedValue));
        }
        finally { if (directory.Exists) directory.Delete(true); }
    }
}
