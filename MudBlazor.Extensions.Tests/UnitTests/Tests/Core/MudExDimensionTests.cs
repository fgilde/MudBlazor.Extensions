using MudBlazor.Extensions.Core;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExDimensionTests
{
    [Fact]
    public void CanCreateDimensionFromSingleSize()
    {
        var size = new MudExSize<double>(100);
        var dimension = new MudExDimension(size);
        
        Assert.Equal(size, dimension.Width);
        Assert.Equal(size, dimension.Height);
    }

    [Fact]
    public void CanCreateDimensionFromWidthAndHeight()
    {
        var width = new MudExSize<double>(100);
        var height = new MudExSize<double>(200);
        var dimension = new MudExDimension(width, height);
        
        Assert.Equal(width, dimension.Width);
        Assert.Equal(height, dimension.Height);
    }

    [Fact]
    public void CanCreateDimensionFromString()
    {
        var dimension = new MudExDimension("100pxx200px");
        
        Assert.Equal("100px", dimension.Width.ToString());
        Assert.Equal("200px", dimension.Height.ToString());
    }

    [Fact]
    public void CanCreateDimensionFromSingleValueString()
    {
        var dimension = new MudExDimension("50%");
        
        Assert.Equal("50%", dimension.Width.ToString());
        Assert.Equal("50%", dimension.Height.ToString());
    }

    [Fact]
    public void ImplicitConversionFromDouble()
    {
        MudExDimension dimension = 100.0;
        
        Assert.Equal(100.0, dimension.Width.Value);
        Assert.Equal(100.0, dimension.Height.Value);
    }

    [Fact]
    public void ImplicitConversionFromString()
    {
        MudExDimension dimension = "100pxx200px";
        
        Assert.Equal("100px", dimension.Width.ToString());
        Assert.Equal("200px", dimension.Height.ToString());
    }

    [Fact]
    public void ToStringReturnsCorrectFormat()
    {
        var dimension = new MudExDimension(new MudExSize<double>(100), new MudExSize<double>(200));
        
        Assert.Equal("100pxx200px", dimension.ToString());
    }

    [Fact]
    public void ToAbsoluteConvertsPercentagesToPixels()
    {
        var dimension = new MudExDimension("50%x75%");
        var reference = new MudExDimension(new MudExSize<double>(200), new MudExSize<double>(400));
        
        var absolute = dimension.ToAbsolute(reference);
        
        Assert.Equal(100.0, absolute.Width.Value);
        Assert.Equal(300.0, absolute.Height.Value);
    }

    [Fact]
    public void ToRelativeConvertsPixelsToPercentages()
    {
        var dimension = new MudExDimension(new MudExSize<double>(100), new MudExSize<double>(200));
        var reference = new MudExDimension(new MudExSize<double>(200), new MudExSize<double>(400));
        
        var relative = dimension.ToRelative(reference);
        
        Assert.Equal(50.0, relative.Width.Value);
        Assert.Equal(50.0, relative.Height.Value);
    }
}
