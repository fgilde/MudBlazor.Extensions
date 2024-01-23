using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using DColor = System.Drawing.Color;
namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExColorTests
{
    [Fact]
    public void Trans()
    {
        var color = MudExColor.Transparent;
        MudExColor color2 = "transparent";

        var s1 = color.ToCssStringValue();
        var s2 = color2.ToCssStringValue();
        Assert.Equal(s1, s2);
    }

    [Fact]
    public void ShouldCreateMudExColorFromColorEnum()
    {
        var color = MudExColor.Transparent;

        Assert.True(color.IsColor);
        Assert.Equal(Color.Transparent, color.AsColor);
        Assert.Equal("transparent", color.ToCssStringValue());
    }

    [Fact]
    public void ShouldCreateMudExColorFromColor()
    {
        var color = new MudExColor(Color.Primary);

        Assert.True(color.IsColor);
        Assert.Equal(Color.Primary, color.AsColor);
        Assert.Equal("var(--mud-palette-primary)", color.ToCssStringValue());
    }

    [Fact]
    public void ShouldCreateMudExColorFromMudColor()
    {
        var mudColor = new MudColor(255, 0, 0, 0);
        var color = new MudExColor(mudColor);

        Assert.True(color.IsMudColor);
        Assert.Equal(mudColor, color.AsMudColor);
    }

    [Fact]
    public void ShouldCreateMudExColorFromDrawingColor()
    {
        var dColor = DColor.FromArgb(255, 0, 0);
        var color = new MudExColor(dColor);

        Assert.True(color.IsDrawingColor);
        Assert.Equal(dColor, color.AsDrawingColor);
    }

    [Fact]
    public void ShouldCreateMudExColorFromString()
    {
        var color = new MudExColor("#ff0000");

        Assert.True(color.IsString);
        Assert.Equal("#ff0000", color.AsString);
    }

    [Fact]
    public void ShouldCreateMudExColorFromUInt()
    {
        var color = new MudExColor(255u);

        Assert.True(color.IsInt);
        Assert.Equal(255u, color.AsInt);
    }

    [Fact]
    public void ShouldConvertColorToMudColor()
    {
        var color = new MudExColor("#ff0000");
        var mudColor = color.ToMudColor();

        Assert.Equal(color.AsString, mudColor.ToString(MudColorOutputFormats.Hex));
    }

    [Fact]
    public void ShouldConvertDrawingColorToMudColor()
    {
        var dColor = DColor.FromArgb(255, 0, 0);
        var color = new MudExColor(dColor);
        var mudColor = color.ToMudColor();

        Assert.Equal(dColor.ToMudColor(), mudColor);
    }

    [Fact]
    public void ShouldConvertColorEnumToMudColor()
    {
        var colorEnum = Color.Primary;
        var color = new MudExColor(colorEnum);
        var mudColor = color.ToMudColor();

        Assert.Equal(colorEnum, color.AsColor);
    }

    [Fact]
    public void ShouldEqualWhenComparingSameColors()
    {
        var color1 = new MudExColor(DColor.FromArgb(255, 0, 0));
        var color2 = new MudExColor(DColor.FromArgb(255, 0, 0));

        Assert.True(color1.Is(color2.AsDrawingColor));
    }

    [Fact]
    public void ShouldNotEqualWhenComparingDifferentColors()
    {
        var color1 = new MudExColor(DColor.FromArgb(255, 0, 0));
        var color2 = new MudExColor(DColor.FromArgb(255, 255, 0));

        Assert.False(color1.Is(color2.AsDrawingColor));
    }

    [Fact]
    public void ShouldConvertToCssStringValue()
    {
        var color = new MudExColor("red");
        var cssValue = color.ToCssStringValue();

        Assert.Equal("#ff0000", cssValue);
    }
}
