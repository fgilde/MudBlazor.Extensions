using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.Tests.Helper;

public class MudExStyleBuilderTests
{
    [Fact]
    public void TestStyleStringGeneration()
    {
        // Arrange
        var testObject = new
        {
            Height = 10,
            width = "50px",
            BackgroundColor = "red"
        };

        // Act
        var result = MudExStyleBuilder.GenerateStyleString(testObject, CssUnit.Rem);

        // Assert
        Assert.Equal("height: 10rem;width: 50px;background-color: red;", result);
    }

    [Fact]
    public void TestCombineStyleStrings()
    {
        // Arrange
        var cssString1 = "height: 100px; width: 50px;";
        var cssString2 = "color: red;";

        // Act
        var result = MudExStyleBuilder.CombineStyleStrings(cssString1, cssString2);

        // Assert
        Assert.Equal("height: 100px; width: 50px; color: red;", result.TrimEnd());
    }

    class TestCss
    {
        public string height { get; set; }
        public string width { get; set; }
        public string color { get; set; }
    }


    [Fact]
    public void TestStyleStringToObjectConversion()
    {
        // Arrange
        var css = "height: 100px; width: 50px; color: red;";

        // Act
        var result = MudExStyleBuilder.StyleStringToObject<TestCss>(css);

        // Assert
        Assert.Equal("100px", result.height);
        Assert.Equal("50px", result.width);
        Assert.Equal("red", result.color);
    }

    [Fact]
    public void TestWithStyleMethod()
    {
        // Arrange
        var builder = new MudExStyleBuilder();
        var css = "height: 100px; width: 50px; color: red;";

        // Act
        builder.WithStyle(css);
        var result = builder.Build();

        // Assert
        Assert.Equal("height: 100px; width: 50px; color: red;", result);
    }

    //[Fact]
    //public void TestBuildAsClassRuleAsync()
    //{
    //    // Arrange
    //    var builder = new MudExStyleBuilder();
    //    var css = "height: 100px; width: 50px; color: red;";
    //    builder.WithStyle(css);

    
    //    var jsRuntime = /* get a mock IJSRuntime here */;

    //    // Act
    //    var className = await builder.BuildAsClassRuleAsync(null, jsRuntime);

    //    // Assert
    //    // Check that the result is as expected
    //}
}