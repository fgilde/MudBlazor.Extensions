using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExSizeTests
{
    [Fact]
    public void CanParsePxString()
    {        
        var sizeStrPx = "32px";

        var size = new MudExSize<double>(sizeStrPx);
        Assert.Equal(32, size.Value);
        Assert.Equal(CssUnit.Pixels, size.SizeUnit);
        
    }

    [Fact]
    public void CanParsePercentageString()
    {
        var sizeStrPer = "37,869%";

        var size = new MudExSize<double>(sizeStrPer);
        Assert.Equal(37.869, size.Value);
        Assert.Equal(CssUnit.Percentage, size.SizeUnit);

        sizeStrPer = "37.869%";        

        size = new MudExSize<double>(sizeStrPer);
        Assert.Equal(37.869, size.Value);
        Assert.Equal(CssUnit.Percentage, size.SizeUnit);

    }


    [Fact]
    public void CanParsePercentageString2()
    {
        var sizeStrPer = "37,869%";

        var size = new MudExSize<double>(sizeStrPer);
        Assert.Equal(37.869, size.Value);
        Assert.Equal(CssUnit.Percentage, size.SizeUnit);

        sizeStrPer = "37.869%";

        size = new MudExSize<double>(sizeStrPer);
        Assert.Equal(37.869, size.Value);
        Assert.Equal(CssUnit.Percentage, size.SizeUnit);
    }

    [Fact]
    public void CanParseEmString()
    {
        var sizeStrEm = "2.5em";

        var size = new MudExSize<double>(sizeStrEm);
        Assert.Equal(2.5, size.Value);
        Assert.Equal(CssUnit.Em, size.SizeUnit);
    }

    [Fact]
    public void CanParseRemString()
    {
        var sizeStrRem = "1.2rem";

        var size = new MudExSize<double>(sizeStrRem);
        Assert.Equal(1.2, size.Value);
        Assert.Equal(CssUnit.Rem, size.SizeUnit);
    }

    [Fact]
    public void CanParseVwString()
    {
        var sizeStrVw = "50vw";

        var size = new MudExSize<double>(sizeStrVw);
        Assert.Equal(50, size.Value);
        Assert.Equal(CssUnit.ViewportWidth, size.SizeUnit);
    }

    [Fact]
    public void CanParseVhString()
    {
        var sizeStrVh = "100vh";

        var size = new MudExSize<double>(sizeStrVh);
        Assert.Equal(100, size.Value);
        Assert.Equal(CssUnit.ViewportHeight, size.SizeUnit);
    }

    [Fact]
    public void CanParseVminString()
    {
        var sizeStrVmin = "75vmin";

        var size = new MudExSize<double>(sizeStrVmin);
        Assert.Equal(75, size.Value);
        Assert.Equal(CssUnit.ViewportMinimum, size.SizeUnit);
    }

    [Fact]
    public void CanParseVmaxString()
    {
        var sizeStrVmax = "80vmax";

        var size = new MudExSize<double>(sizeStrVmax);
        Assert.Equal(80, size.Value);
        Assert.Equal(CssUnit.ViewportMaximum, size.SizeUnit);
    }

    [Fact]
    public void CanParseInchString()
    {
        var sizeStrIn = "5.5in";

        var size = new MudExSize<double>(sizeStrIn);
        Assert.Equal(5.5, size.Value);
        Assert.Equal(CssUnit.Inches, size.SizeUnit);
    }

    [Fact]
    public void CanParsePtString()
    {
        var sizeStrPt = "10.5pt";

        var size = new MudExSize<double>(sizeStrPt);
        Assert.Equal(10.5, size.Value);
        Assert.Equal(CssUnit.Points, size.SizeUnit);
    }

    [Fact]
    public void CanParsePcString()
    {
        var sizeStrPc = "8.5pc";

        var size = new MudExSize<double>(sizeStrPc);
        Assert.Equal(8.5, size.Value);
        Assert.Equal(CssUnit.Picas, size.SizeUnit);
    }

    [Fact]
    public void CanParseEmptyString()
    {
        var sizeStrEmpty = "";

        var size = new MudExSize<double>(sizeStrEmpty);
        Assert.Equal(0, size.Value);
        Assert.Equal(CssUnit.Pixels, size.SizeUnit);
    }

    [Fact]
    public void ThrowsExceptionForInvalidString()
    {
        var sizeStrInvalid = "invalid";

        Assert.Throws<ArgumentException>(() => new MudExSize<double>(sizeStrInvalid));
    }

}