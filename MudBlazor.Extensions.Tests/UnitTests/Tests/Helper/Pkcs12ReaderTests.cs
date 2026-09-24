using System.Security.Cryptography;
using MudBlazor.Extensions.Helper.Internal;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

/// <summary>
/// Covers reading a password protected PKCS#12 container without the platform certificate stack - the thing
/// a Blazor WebAssembly app cannot do through X509Certificate2.
/// </summary>
public class Pkcs12ReaderTests
{
    private static string SampleDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Samples")))
                directory = directory.Parent;

            return Path.Combine(directory!.FullName, "Samples", "MainSample.WebAssembly", "wwwroot", "sample-data");
        }
    }

    private static byte[] Sample(string name) => File.ReadAllBytes(Path.Combine(SampleDirectory, name));

    [Fact]
    public async Task TheCertificateOfAContainerIsRead()
    {
        // No js runtime: on a normal runtime the managed path does all of it, the browser path is not needed.
        var certificates = await Pkcs12CertificateReader.ReadCertificatesAsync(Sample("sample.pfx"), "mudex", null);

        var der = Assert.Single(certificates);
        var details = X509CertificateReader.Read(der).Single();

        Assert.Contains("MudEx Sample Certificate", details.Subject);
        Assert.Contains("MudBlazor.Extensions", details.Issuer);
        Assert.True(details.NotAfter > DateTimeOffset.Now);
    }

    [Fact]
    public async Task AWrongPasswordIsReported()
    {
        await Assert.ThrowsAsync<CryptographicException>(
            () => Pkcs12CertificateReader.ReadCertificatesAsync(Sample("sample.pfx"), "wrong", null));
    }

    [Fact]
    public void AContainerIsToldApartFromACertificate()
    {
        Assert.True(Pkcs12CertificateReader.IsPkcs12(Sample("sample.pfx")));
        Assert.False(Pkcs12CertificateReader.IsPkcs12(Sample("sample.pem")));
        Assert.False(Pkcs12CertificateReader.IsPkcs12(new byte[] { 1, 2, 3 }));
        Assert.False(Pkcs12CertificateReader.IsPkcs12(null));
    }
}
