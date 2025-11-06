using MudBlazor.Extensions.Core;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExPositionTests
{
    [Fact]
    public void CanCreatePositionFromSingleSize()
    {
        var size = new MudExSize<double>(50);
        var position = new MudExPosition(size);
        
        Assert.Equal(size, position.Left);
        Assert.Equal(size, position.Top);
    }

    [Fact]
    public void CanCreatePositionFromLeftAndTop()
    {
        var left = new MudExSize<double>(100);
        var top = new MudExSize<double>(200);
        var position = new MudExPosition(left, top);
        
        Assert.Equal(left, position.Left);
        Assert.Equal(top, position.Top);
    }

    [Fact]
    public void CanCreatePositionFromString()
    {
        var position = new MudExPosition("10pxx20px");
        
        Assert.Equal("10px", position.Left.ToString());
        Assert.Equal("20px", position.Top.ToString());
    }

    [Fact]
    public void CanCreatePositionFromSingleValueString()
    {
        var position = new MudExPosition("25%");
        
        Assert.Equal("25%", position.Left.ToString());
        Assert.Equal("25%", position.Top.ToString());
    }

    [Fact]
    public void ImplicitConversionFromDouble()
    {
        MudExPosition position = 150.0;
        
        Assert.Equal(150.0, position.Left.Value);
        Assert.Equal(150.0, position.Top.Value);
    }

    [Fact]
    public void ImplicitConversionFromString()
    {
        MudExPosition position = "50pxx100px";
        
        Assert.Equal("50px", position.Left.ToString());
        Assert.Equal("100px", position.Top.ToString());
    }

    [Fact]
    public void ToStringReturnsCorrectFormat()
    {
        var position = new MudExPosition(new MudExSize<double>(75), new MudExSize<double>(125));
        
        Assert.Equal("75pxx125px", position.ToString());
    }

    [Fact]
    public void ToAbsoluteConvertsPercentagesToPixels()
    {
        var position = new MudExPosition("25%x50%");
        var reference = new MudExDimension(new MudExSize<double>(400), new MudExSize<double>(600));
        
        var absolute = position.ToAbsolute(reference);
        
        Assert.Equal(100.0, absolute.Left.Value);
        Assert.Equal(300.0, absolute.Top.Value);
    }

    [Fact]
    public void ToRelativeConvertsPixelsToPercentages()
    {
        var position = new MudExPosition(new MudExSize<double>(150), new MudExSize<double>(300));
        var reference = new MudExDimension(new MudExSize<double>(600), new MudExSize<double>(1200));
        
        var relative = position.ToRelative(reference);
        
        Assert.Equal(25.0, relative.Left.Value);
        Assert.Equal(25.0, relative.Top.Value);
    }
}
