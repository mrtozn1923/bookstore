using Bookstore.Crypto;

namespace Bookstore.Crypto.Tests;

public class CryptoRequirementsTests
{
    [Fact]
    public void Missing_algorithm_fails_at_startup_with_its_name()
    {
        CryptoCapability[] capabilities = [new("AES-GCM", true), new("ML-KEM", false)];

        PlatformNotSupportedException ex = Assert.Throws<PlatformNotSupportedException>(
            () => CryptoRequirements.EnsureSupported(capabilities, ["AES-GCM", "ML-KEM"]));

        Assert.Contains("ML-KEM", ex.Message);
        Assert.DoesNotContain("AES-GCM,", ex.Message);
    }

    [Fact]
    public void Bookstore_requirements_hold_on_this_platform() =>
        CryptoRequirements.EnsureSupported(CryptoCapabilities.Probe(), CryptoRequirements.Bookstore);
}
