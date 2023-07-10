using Xunit;
using MudBlazor.Extensions.Helper;
using System.Collections.Generic;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;
public class MudExCssBuilderTests
{
    [Fact]
    public void CanAddClassToBuilder()
    {
        var cssBuilder = MudExCssBuilder.Default;
        cssBuilder.AddClass("testClass");

        Assert.Equal("testClass", cssBuilder.Class);
    }

    [Fact]
    public void CanAddMultipleClassesToBuilder()
    {
        var cssBuilder = MudExCssBuilder.Default;
        cssBuilder.AddClass("testClass", "anotherTestClass");

        Assert.Equal("testClass anotherTestClass", cssBuilder.Class);
    }

    [Fact]
    public void CanAddClassToBuilderConditionally()
    {
        var cssBuilder = MudExCssBuilder.Default;
        cssBuilder.AddClass("testClass", true);
        cssBuilder.AddClass("shouldNotBeAdded", false);

        Assert.Equal("testClass", cssBuilder.Class);
    }

    [Fact]
    public async void CanRemoveClassFromBuilderAsync()
    {
        var cssBuilder = MudExCssBuilder.Default;
        cssBuilder.AddClass("testClass");
        await cssBuilder.RemoveClassesAsync("testClass");

        Assert.Equal("", cssBuilder.Class);
    }

    [Fact]
    public void CanAddClassFromAttributes()
    {
        var cssBuilder = MudExCssBuilder.Default;
        var attributes = new Dictionary<string, object> { { "class", "testClass" } };

        cssBuilder.AddClassFromAttributes(attributes);

        Assert.Equal("testClass", cssBuilder.Class);
    }

    [Fact]
    public void EmptyClassOnInitialization()
    {
        var cssBuilder = MudExCssBuilder.Default;

        Assert.Equal("", cssBuilder.Class);
    }
}