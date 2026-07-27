using System.Linq;
using NUnit.Framework;
using Try.Core;

namespace Try.Tests;

[TestFixture]
public class NugetPackageHelperTests
{
    [Test]
    public void SelectBestLibFolder_PrefersHighestTfm()
    {
        var entries = new[]
        {
            "lib/netstandard2.0/Foo.dll",
            "lib/net6.0/Foo.dll",
            "lib/net472/Foo.dll",
        };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/net6.0/"));
    }

    [Test]
    public void SelectBestLibFolder_FallsBackToNetstandard()
    {
        var entries = new[] { "lib/netstandard2.0/Foo.dll", "lib/net472/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/netstandard2.0/"));
    }

    [Test]
    public void SelectBestLibFolder_IgnoresPlatformSpecificTfms()
    {
        var entries = new[] { "lib/net6.0-windows/Foo.dll", "lib/netstandard2.0/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/netstandard2.0/"));
    }

    [Test]
    public void SelectBestLibFolder_ReturnsNullWhenOnlyIncompatible()
    {
        var entries = new[] { "lib/net472/Foo.dll", "ref/net6.0/Foo.dll", "analyzers/dotnet/cs/Gen.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.Null);
    }

    [Test]
    public void SelectBestLibFolder_ReturnsNullForMetaPackageWithoutLib()
    {
        var entries = new[] { "Humanizer.nuspec", "_rels/.rels" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.Null);
    }

    [Test]
    public void SelectBestLibFolder_FlatLibFolder()
    {
        var entries = new[] { "lib/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("lib/"));
    }

    [Test]
    public void SelectBestLibFolder_IsCaseInsensitive()
    {
        var entries = new[] { "Lib/NetStandard2.0/Foo.dll" };
        Assert.That(NugetPackageHelper.SelectBestLibFolder(entries), Is.EqualTo("Lib/NetStandard2.0/"));
    }
}
