using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Tests.Tests.Helper;

public class MudExColorUtilsTests
{
    [Fact]
    public void ToInt_ShouldConvertMudColorToInt()
    {
        var mudColor = new MudColor(255, 127, 0, 255); // some arbitrary MudColor instance
        var result = mudColor.ToInt();
        Assert.Equal(-33024, result); // The expected integer value
    }

    [Fact]
    public void CssVarName_ShouldReturnCorrectCssVariableName()
    {
        var color = Color.Primary; // Assuming Color is an enum with a value "Red"
        var result = color.CssVarName();
        Assert.Equal("--mud-palette-primary", result);
    }

    [Fact]
    public void ToDrawingColor_ShouldConvertMudColorToDrawingColor()
    {
        var mudColor = new MudColor(255, 127, 0, 255); // some arbitrary MudColor instance
        var result = mudColor.ToDrawingColor();
        Assert.Equal(System.Drawing.Color.FromArgb(255, 255, 127, 0), result); // Assuming Color is System.Drawing.Color
    }

    [Fact]
    public void IsBlack_ShouldReturnTrueForBlackMudColor()
    {
        var mudColor = new MudColor(0, 0, 0, 255); // Black MudColor
        var result = mudColor.IsBlack();
        Assert.True(result);
    }

    [Fact]
    public void IsBlack_ShouldReturnFalseForNonBlackMudColor()
    {
        var mudColor = new MudColor(255, 255, 255, 255); // White MudColor
        var result = mudColor.IsBlack();
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ShouldReturnTrueForValidMudColor()
    {
        var mudColor = new MudColor(255, 255, 255, 255); // Valid MudColor
        var result = mudColor.IsValid();
        Assert.True(result);
    }

    [Fact]
    public void TryParseFromHtmlColorName_ShouldParseHtmlColorNameToDrawingColor()
    {
        string htmlColorName = "Red";
        System.Drawing.Color color;
        bool result = MudExColorUtils.TryParseFromHtmlColorName(htmlColorName, out color);
        Assert.True(result);
        Assert.Equal(System.Drawing.Color.Red, color); // Assuming Color is System.Drawing.Color
    }
}