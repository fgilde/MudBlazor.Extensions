using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExAppearanceTests
{
    [Fact]
    public void EmptyCreatesNewInstance()
    {
        var appearance = MudExAppearance.Empty();
        
        Assert.NotNull(appearance);
        Assert.Equal(string.Empty, appearance.Class);
        Assert.Equal(string.Empty, appearance.Style);
        Assert.True(appearance.KeepExisting);
    }

    [Fact]
    public void FromCssCreatesInstanceWithClass()
    {
        var appearance = MudExAppearance.FromCss(MudExCss.Classes.Backgrounds.Blur);
        
        Assert.NotNull(appearance);
        Assert.NotEmpty(appearance.Class);
    }

    [Fact]
    public void FromCssBuilderCreatesInstanceWithClass()
    {
        var builder = MudExCssBuilder.Default.AddClass("test-class");
        var appearance = MudExAppearance.FromCss(builder);
        
        Assert.NotNull(appearance);
        Assert.Contains("test-class", appearance.Class);
    }

    [Fact]
    public void FromStyleStringCreatesInstanceWithStyle()
    {
        var appearance = MudExAppearance.FromStyle("color: red;");
        
        Assert.NotNull(appearance);
        Assert.Contains("color", appearance.Style);
        Assert.Contains("red", appearance.Style);
    }

    [Fact]
    public void FromStyleBuilderCreatesInstanceWithStyle()
    {
        var styleBuilder = MudExStyleBuilder.Empty().WithColor("blue");
        var appearance = MudExAppearance.FromStyle(styleBuilder);
        
        Assert.NotNull(appearance);
        Assert.Contains("color", appearance.Style);
    }

    [Fact]
    public void FromStyleActionCreatesInstanceWithStyle()
    {
        var appearance = MudExAppearance.FromStyle(s => s.WithColor("green"));
        
        Assert.NotNull(appearance);
        Assert.Contains("color", appearance.Style);
    }

    [Fact]
    public void WithStyleAddsStyleToAppearance()
    {
        var appearance = MudExAppearance.Empty();
        appearance.WithStyle("color: yellow;");
        
        Assert.Contains("color", appearance.Style);
        Assert.Contains("yellow", appearance.Style);
    }

    [Fact]
    public void WithCssAddsClassToAppearance()
    {
        var appearance = MudExAppearance.Empty();
        appearance.WithCss("custom-class");
        
        Assert.Contains("custom-class", appearance.Class);
    }

    [Fact]
    public void CloneCreatesNewInstance()
    {
        var original = MudExAppearance.FromStyle("color: red;").WithCss("test-class");
        var clone = (MudExAppearance)original.Clone();
        
        Assert.NotSame(original, clone);
        Assert.Equal(original.Class, clone.Class);
        Assert.Equal(original.Style, clone.Style);
        Assert.Equal(original.KeepExisting, clone.KeepExisting);
    }

    [Fact]
    public void KeepExistingDefaultsToTrue()
    {
        var appearance = MudExAppearance.Empty();
        
        Assert.True(appearance.KeepExisting);
    }

    [Fact]
    public void CanSetKeepExistingToFalse()
    {
        var appearance = MudExAppearance.Empty();
        appearance.KeepExisting = false;
        
        Assert.False(appearance.KeepExisting);
    }

    [Fact]
    public void WithStyleFromObjectAddsStyles()
    {
        var styleObj = new { Color = "red", FontSize = "14px" };
        var appearance = MudExAppearance.Empty().WithStyle(styleObj);
        
        Assert.NotEmpty(appearance.Style);
    }

    [Fact]
    public void WithStyleFromStyleAppearanceAddsStyle()
    {
        var styleAppearance = MudExAppearance.FromStyle("margin: 10px;");
        var appearance = MudExAppearance.Empty().WithStyle(styleAppearance);
        
        Assert.Contains("margin", appearance.Style);
    }

    [Fact]
    public void MultipleWithStyleCallsAccumulateStyles()
    {
        var appearance = MudExAppearance.Empty()
            .WithStyle("color: red;")
            .WithStyle("margin: 10px;");
        
        Assert.Contains("color", appearance.Style);
        Assert.Contains("margin", appearance.Style);
    }

    [Fact]
    public void MultipleWithCssCallsAccumulateClasses()
    {
        var appearance = MudExAppearance.Empty()
            .WithCss("class-1")
            .WithCss("class-2");
        
        Assert.Contains("class-1", appearance.Class);
        Assert.Contains("class-2", appearance.Class);
    }
}
