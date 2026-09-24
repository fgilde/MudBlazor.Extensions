using System.Security.Cryptography;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Helper.Internal.Crypto;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

/// <summary>
/// Covers the old pfx encryption schemes, which no current .NET can open: RC2 went away with .NET Core and
/// the browser runtime has no symmetric cipher at all.
/// </summary>
/// <remarks>
/// The containers were written by openssl, one per scheme, and hold the same certificate. Reading the subject
/// back out of them is what proves the key derivation and the cipher are right - one wrong bit and the padding
/// check already fails. The known answer tests pin the ciphers themselves against their standards.
/// </remarks>
public class LegacyPkcs12Tests
{
    private const string Password = "mudex";

    private static byte[] Container(string name)
        => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Pkcs12", name));

    [Theory]
    [InlineData("tripledes.pfx")]
    [InlineData("rc2_40.pfx")]
    [InlineData("rc2_128.pfx")]
    public async Task ALegacyContainerIsRead(string name)
    {
        var certificates = await Pkcs12CertificateReader.ReadCertificatesAsync(Container(name), Password, null);

        var details = X509CertificateReader.Read(certificates.Single()).Single();
        Assert.Contains("MudEx Sample Certificate", details.Subject);
        Assert.Contains("MudBlazor.Extensions", details.Issuer);
    }

    [Theory]
    [InlineData("tripledes.pfx")]
    [InlineData("rc2_40.pfx")]
    [InlineData("rc2_128.pfx")]
    public async Task AWrongPasswordIsRefused(string name)
    {
        await Assert.ThrowsAsync<CryptographicException>(
            () => Pkcs12CertificateReader.ReadCertificatesAsync(Container(name), "wrong", null));
    }

    [Fact]
    public void DesMatchesItsKnownAnswer()
    {
        // FIPS 81: key 0123456789abcdef turns "Now is t" into 3fa40e8a984d4815. Triple DES with the same key
        // three times is single DES, so this pins the block cipher itself.
        var key = Convert.FromHexString(string.Concat(Enumerable.Repeat("0123456789abcdef", 3)));
        var cipher = Convert.FromHexString("3fa40e8a984d4815");

        var plain = TripleDesCipher.DecryptCbcRaw(key, new byte[8], cipher);

        Assert.Equal("Now is t", System.Text.Encoding.ASCII.GetString(plain));
    }

    [Theory]
    // RFC 2268, section 5.
    [InlineData("0000000000000000", 63, "ebb773f993278eff", "0000000000000000")]
    [InlineData("ffffffffffffffff", 64, "278b27e42e2f0d49", "ffffffffffffffff")]
    // This one verified against openssl rc2-ecb with the same key.
    [InlineData("88bca90e90875a7f0f79c384627bafb2", 128, "2269552ab0f85ca6", "0000000000000000")]
    public void Rc2MatchesItsKnownAnswers(string key, int effectiveBits, string cipher, string expected)
    {
        var plain = Rc2Cipher.DecryptBlockRaw(Convert.FromHexString(key), effectiveBits, Convert.FromHexString(cipher));

        Assert.Equal(expected, Convert.ToHexString(plain).ToLowerInvariant());
    }

    [Fact]
    public void TheKeyDerivationMatchesTheContainer()
    {
        // Same container, same password, same derived material - reading it twice has to agree with itself.
        var first = Pkcs12KeyDerivation.Derive(Password, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 2048,
            Pkcs12KeyDerivation.Purpose.Key, 24);
        var second = Pkcs12KeyDerivation.Derive(Password, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 2048,
            Pkcs12KeyDerivation.Purpose.Key, 24);
        var iv = Pkcs12KeyDerivation.Derive(Password, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 2048,
            Pkcs12KeyDerivation.Purpose.InitialisationVector, 8);

        Assert.Equal(first, second);
        // Key and iv come from the same password but a different diversifier, so they must differ.
        Assert.NotEqual(first.Take(8), iv);
    }
}
