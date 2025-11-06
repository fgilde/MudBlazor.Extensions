using MudBlazor.Extensions.Helper;
using System.ComponentModel;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

public class EnumHelperTests
{
    private enum TestEnum
    {
        [Description("First Value")]
        Value1,
        
        [Description("Second Value")]
        Value2,
        
        Value3
    }

    [Fact]
    public void GetCustomAttributesReturnsAttributes()
    {
        var attributes = EnumHelper.GetCustomAttributes<DescriptionAttribute>(TestEnum.Value1, false);
        
        Assert.NotEmpty(attributes);
        Assert.Equal("First Value", attributes[0].Description);
    }

    [Fact]
    public void GetCustomAttributesReturnsMultipleAttributesIfPresent()
    {
        var attributes = EnumHelper.GetCustomAttributes<DescriptionAttribute>(TestEnum.Value2, false);
        
        Assert.NotEmpty(attributes);
        Assert.Equal("Second Value", attributes[0].Description);
    }

    [Fact]
    public void GetCustomAttributesReturnsEmptyForNoAttributes()
    {
        var attributes = EnumHelper.GetCustomAttributes<DescriptionAttribute>(TestEnum.Value3, false);
        
        Assert.Empty(attributes);
    }

    [Fact]
    public void GetCustomAttributesReturnsEmptyArrayForNonExistentAttribute()
    {
        var attributes = EnumHelper.GetCustomAttributes<ObsoleteAttribute>(TestEnum.Value1, false);
        
        Assert.Empty(attributes);
    }
}
