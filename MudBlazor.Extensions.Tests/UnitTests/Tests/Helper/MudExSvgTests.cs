using MudBlazor.Extensions.Helper;
using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

public class MudExSvgTests
{
    [Fact]
    public void CombineIconsHorizontal_ShouldReturnCombinedSvgImages_AlignedHorizontally()
    {
        // Arrange
        var image = "<circle cx='5' cy='5' r='5'/>";
        var other = new[] { "<rect x='12' y='0' width='10' height='10'/>" };

        // Act
        var result = MudExSvg.CombineIconsHorizontal(image, other);

        // Assert
        const string expected = @"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 36 10'>
  <g transform='translate(0, 0)'>
    <circle cx='5' cy='5' r='5'/>
  </g>
  <g transform='translate(14, 0)'>
    <rect x='12' y='0' width='10' height='10'/>
  </g>
</svg>";

        Assert.Equal(Regex.Replace(expected.Replace(Environment.NewLine, ""), "> <", "><"), Regex.Replace(expected.Replace(Environment.NewLine, ""), "> <", "><"), ignoreLineEndingDifferences: true);

    }

    [Fact]
    public void CombineIconsVertical_ShouldReturnCombinedSvgImages_AlignedVertically()
    {
        // Arrange
        var image = "<circle cx='5' cy='5' r='5'/>";
        var other = new[] { "<rect x='0' y='12' width='10' height='10'/>" };

        // Act
        var result = MudExSvg.CombineIconsVertical(image, other);

        // Assert
        const string expected = @"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 36'>
  <g transform='translate(0, 0)'>
    <circle cx='5' cy='5' r='5'/>
  </g>
  <g transform='translate(0, 14)'>
    <rect x='0' y='12' width='10' height='10'/>
  </g>
</svg>";


        Assert.Equal(Regex.Replace(expected.Replace(Environment.NewLine, ""), "> <", "><"), Regex.Replace(expected.Replace(Environment.NewLine, ""), "> <", "><"), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void CombineIconsCentered_ShouldReturnCombinedSvgImages_CenteredOverlapped()
    {
        // Arrange
        var image = "<circle cx='5' cy='5' r='5'/>";
        var other = new[] { "<rect x='2' y='2' width='11' height='11'/>" };

        // Act
        var result = MudExSvg.CombineIconsCentered(image, other);

        // Assert
        const string expected = @"<svg xmlns='http://www.w3.org/2000/svg'>
  <g transform='translate(0, 0)'>
    <circle cx='5' cy='5' r='5'/>
  </g>
  <g transform='translate(0, 0)'>
    <rect x='2' y='2' width='11' height='11'/>
  </g>
</svg>";

        Assert.Equal(Regex.Replace(expected.Replace(Environment.NewLine, ""), "> <", "><"), Regex.Replace(expected.Replace(Environment.NewLine, ""), "> <", "><"), ignoreLineEndingDifferences: true);

    }

    [Fact]
    public void SvgPropertyNameForValue_ShouldReturnFullyQualifiedNameOfConstant_WhenPassedValueAndSingleOwnerType()
    {
        // Act
        var result = MudExSvg.SvgPropertyNameForValue(Icons.Outlined.Search, typeof(Icons));

        // Assert
        Assert.Equal("MudBlazor.Icons.Outlined.Search", result);
    }

    [Fact]
    public void SvgPropertyNameForValue_ShouldReturnFullyQualifiedNameOfConstant_WhenPassedValueAndMultipleOwnerTypes()
    {
        // Act
        var result = MudExSvg.SvgPropertyNameForValue(Icons.Outlined.Search, typeof(Icons), typeof(OtherIcons));

        // Assert
        Assert.Equal("MudBlazor.Icons.Outlined.Search", result);
    }

    [Fact]
    public void SvgPropertyNameForValue_ShouldReturnFullyQualifiedNameOfConstant_WhenPassedValueAndMultipleOwnerTypesInArray()
    {
        // Arrange
        var allOwnerTypes = new[] { typeof(Icons), typeof(OtherIcons) };

        // Act
        var result = MudExSvg.SvgPropertyNameForValue(Icons.Outlined.Search, allOwnerTypes);

        // Assert
        Assert.Equal("MudBlazor.Icons.Outlined.Search", result);
    }

    [Fact]
    public void SvgPropertyNameForValue_ShouldReturnNull_WhenPassedNonExistentValue()
    {
        // Act
        var result = MudExSvg.SvgPropertyNameForValue("nonexistentValue", typeof(Icons));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void SvgPropertyValueForName_ShouldReturnValueOfConstant_WhenPassedFullyQualifiedNameOfConstant()
    {
        // Act
        var result = MudExSvg.SvgPropertyValueForName("MudBlazor.Icons.Outlined.Search");

        // Assert
        Assert.Equal(Icons.Outlined.Search, result);
    }

    [Fact]
    public void SvgPropertyValueForName_ShouldReturnNull_WhenPassedNonExistentFullyQualifiedName()
    {
        // Act
        var result = MudExSvg.SvgPropertyValueForName("MudBlazor.Icons.NonexistentIcon");

        // Assert
        Assert.Null(result);
    }

    private static class OtherIcons
    {
        public const string Search = "Other.Search";
    }
}