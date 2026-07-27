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

    private const string NuspecWithGroups = """
        <?xml version="1.0" encoding="utf-8"?>
        <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
          <metadata>
            <id>Humanizer</id>
            <version>2.14.1</version>
            <dependencies>
              <group targetFramework=".NETFramework4.8">
                <dependency id="OnlyForNetFx" version="1.0.0" />
              </group>
              <group targetFramework=".NETStandard2.0">
                <dependency id="Humanizer.Core" version="[2.14.1]" />
                <dependency id="Humanizer.Core.af" version="[2.14.1]" />
              </group>
            </dependencies>
          </metadata>
        </package>
        """;

    [Test]
    public void GetDependencies_PicksBestMatchingGroup()
    {
        var deps = NugetPackageHelper.GetDependencies(NuspecWithGroups);
        Assert.That(deps.Select(d => d.Id), Is.EquivalentTo(new[] { "Humanizer.Core", "Humanizer.Core.af" }));
        Assert.That(deps.All(d => d.Version == "2.14.1"), Is.True);
    }

    [Test]
    public void GetDependencies_GroupWithoutTargetFrameworkIsFallback()
    {
        const string nuspec = """
            <?xml version="1.0"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
              <metadata>
                <id>X</id><version>1.0.0</version>
                <dependencies>
                  <group>
                    <dependency id="Newtonsoft.Json" version="13.0.1" />
                  </group>
                </dependencies>
              </metadata>
            </package>
            """;
        var deps = NugetPackageHelper.GetDependencies(nuspec);
        Assert.That(deps.Single().Id, Is.EqualTo("Newtonsoft.Json"));
        Assert.That(deps.Single().Version, Is.EqualTo("13.0.1"));
    }

    [Test]
    public void GetDependencies_FlatDependenciesWithoutGroups()
    {
        const string nuspec = """
            <?xml version="1.0"?>
            <package>
              <metadata>
                <id>Old</id><version>1.0.0</version>
                <dependencies>
                  <dependency id="A" version="1.2.3" />
                </dependencies>
              </metadata>
            </package>
            """;
        var deps = NugetPackageHelper.GetDependencies(nuspec);
        Assert.That(deps.Single(), Is.EqualTo(new NugetDependency("A", "1.2.3")));
    }

    [Test]
    public void GetDependencies_NoDependencies_ReturnsEmpty()
    {
        const string nuspec = """
            <?xml version="1.0"?>
            <package><metadata><id>X</id><version>1.0.0</version></metadata></package>
            """;
        Assert.That(NugetPackageHelper.GetDependencies(nuspec), Is.Empty);
    }

    [TestCase("2.14.1", "2.14.1")]
    [TestCase("[2.14.1, 3.0)", "2.14.1")]
    [TestCase("[1.0.0]", "1.0.0")]
    [TestCase("(, 5.0]", null)]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void ParseMinVersion_Cases(string range, string expected)
    {
        Assert.That(NugetPackageHelper.ParseMinVersion(range), Is.EqualTo(expected));
    }

    [TestCase("System.Text.Json", true)]
    [TestCase("Microsoft.Extensions.Logging", true)]
    [TestCase("Microsoft.AspNetCore.Components.Web", true)]
    [TestCase("Microsoft.NETCore.Platforms", true)]
    [TestCase("NETStandard.Library", true)]
    [TestCase("runtime.native.System", true)]
    [TestCase("Microsoft.CSharp", true)]
    [TestCase("Humanizer.Core", false)]
    [TestCase("Newtonsoft.Json", false)]
    [TestCase("Blazored.LocalStorage", false)]
    public void IsFrameworkPackage_Cases(string id, bool expected)
    {
        Assert.That(NugetPackageHelper.IsFrameworkPackage(id), Is.EqualTo(expected));
    }
}
