using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

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
        public string Height { get; set; }
        public string Width { get; set; }
        public string Color { get; set; }
    }


    [Fact]
    public void TestStyleStringToObjectConversion()
    {
        // Arrange
        var css = "height: 100px; width: 50px; color: red;";

        // Act
        var result = MudExStyleBuilder.StyleStringToObject<TestCss>(css);

        // Assert
        Assert.Equal("100px", result.Height);
        Assert.Equal("50px", result.Width);
        Assert.Equal("red", result.Color);
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


    [Fact]
    public void Test_Default_Instance()
    {
        // Arrange & Act
        var builder = MudExStyleBuilder.Default;

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void Test_Empty_Instance()
    {
        // Arrange & Act
        var builder = MudExStyleBuilder.Empty();

        // Assert
        Assert.NotNull(builder);
    }


    [Fact]
    public void Test_FromStyle_Instance()
    {
        // Arrange
        var style = "background-color: var(--mud-indigo-500);";

        // Act
        var builder = MudExStyleBuilder.FromStyle(style);

        // Assert
        Assert.NotNull(builder);
        Assert.Equal(style, builder.Build());
    }


    [Fact]
    public void Test_CombineStyleStrings_Instance()
    {
        // Arrange
        var css1 = "background-color: var(--mud-indigo-500);";
        var css2 = "color: #c0ffee;";

        // Act
        var combinedCss = MudExStyleBuilder.CombineStyleStrings(css1, css2);

        // Assert
        Assert.Equal("background-color: var(--mud-indigo-500); color: #c0ffee; ", combinedCss);
    }


    [Fact]
    public void Test_Build_Default()
    {
        // Arrange
        var builder = MudExStyleBuilder.Default;

        // Act
        var style = builder.Build();

        // Assert
        Assert.Equal("", style);
    }

    [Fact]
    public void Test_Build_WithStyles()
    {
        // Arrange
        var builder = MudExStyleBuilder.Default;
        builder.WithStyle("background-color: #c0ffee;");

        // Act
        var style = builder.Build();

        // Assert
        Assert.Equal("background-color: #c0ffee;", style);
    }

    [Fact]
    public void Test_WithStyle_Default()
    {
        // Arrange
        var builder = MudExStyleBuilder.Default;

        // Act
        var newBuilder = builder.WithStyle("background-color: #c0ffee;");

        // Assert
        Assert.Equal("background-color: #c0ffee;", newBuilder.Build());
    }

    [Fact]
    public void Test_WithStyle_Additional()
    {
        // Arrange
        var builder = MudExStyleBuilder.Default;
        builder.WithStyle("background-color: #c0ffee;");

        // Act
        var newBuilder = builder.WithStyle("color: #00ff00;");

        // Assert
        Assert.Equal("background-color: #c0ffee; color: #00ff00;", newBuilder.Build());
    }

    [Fact]
    public void Test_WithBackground()
    {
        // Arrange
        var builder = MudExStyleBuilder.Default.WithBackground(Color.Primary);

        // Assert
        Assert.Equal("background: var(--mud-palette-primary);", builder.Build());
    }

}