using NUnit.Framework;
using Playzor.Core;

namespace Try.Tests;

[TestFixture]
public class BrandTests
{
    [TestCase("try.mudex.org", "mudex")]
    [TestCase("www.mudex.org", "mudex")]
    [TestCase("playzor.net", "playzor")]
    [TestCase("www.playzor.net", "playzor")]
    [TestCase("staging.playzor.net", "playzor")]
    [TestCase("playzor.de", "playzor-de")]
    [TestCase("www.playzor.de", "playzor-de")]
    [TestCase("localhost", "mudex")]
    [TestCase("localhost:5000", "mudex")]
    [TestCase("", "mudex")]
    [TestCase(null, "mudex")]
    public void FromHost_MapsKnownDomains(string host, string expectedKey)
    {
        Assert.That(Brand.FromHost(host).Key, Is.EqualTo(expectedKey));
    }

    [Test]
    public void FromHost_OverrideWins()
    {
        Assert.That(Brand.FromHost("localhost", "playzor").Key, Is.EqualTo("playzor"));
        Assert.That(Brand.FromHost("playzor.net", "mudex").Key, Is.EqualTo("mudex"));
    }

    [Test]
    public void FromHost_UnknownOverrideIsIgnored()
    {
        Assert.That(Brand.FromHost("playzor.net", "nonsense").Key, Is.EqualTo("playzor"));
    }

    [Test]
    public void GermanBrand_UsesGermanCultureAndPlayzorDefaults()
    {
        Assert.That(Brand.PlayzorDe.Culture, Is.EqualTo("de"));
        Assert.That(Brand.PlayzorDe.Name, Is.EqualTo("Playzor"));
        Assert.That(Brand.PlayzorDe.DefaultSnippet, Is.EqualTo(Brand.Playzor.DefaultSnippet));
    }

    [Test]
    public void MudExBrand_KeepsMudBlazorDefaults()
    {
        Assert.That(Brand.MudEx.DefaultPackages, Does.Contain("MudBlazor.Extensions"));
        Assert.That(Brand.MudEx.Culture, Is.EqualTo("en"));
    }
}
