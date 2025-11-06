using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

public class MudExJsonHelperTests
{
    [Fact]
    public void FormatJsonFormatsValidJson()
    {
        var json = "{\"name\":\"test\",\"value\":123}";
        
        var result = MudExJsonHelper.FormatJson(json);
        
        Assert.NotNull(result);
        Assert.Contains("name", result);
        Assert.Contains("test", result);
        Assert.Contains("value", result);
        // Formatted JSON should have line breaks
        Assert.Contains("\n", result);
    }

    [Fact]
    public void FormatDataStringIfJsonFormatsWhenDataIsJson()
    {
        var json = "{\"name\":\"test\"}";
        
        var result = MudExJsonHelper.FormatDataStringIfJson(json);
        
        Assert.Contains("\n", result);
    }

    [Fact]
    public void FormatDataStringIfJsonReturnsOriginalWhenNotJson()
    {
        var notJson = "This is not JSON";
        
        var result = MudExJsonHelper.FormatDataStringIfJson(notJson);
        
        Assert.Equal(notJson, result);
    }

    [Fact]
    public void MergeJsonCombinesTwoJsonObjects()
    {
        var json1 = "{\"name\":\"test\"}";
        var json2 = "{\"value\":123}";
        
        var result = MudExJsonHelper.MergeJson(json1, json2);
        
        Assert.Contains("name", result);
        Assert.Contains("value", result);
    }

    [Fact]
    public void MergeJsonHandlesMultipleJsonObjects()
    {
        var json1 = "{\"a\":1}";
        var json2 = "{\"b\":2}";
        var json3 = "{\"c\":3}";
        
        var result = MudExJsonHelper.MergeJson(json1, json2, json3);
        
        Assert.Contains("a", result);
        Assert.Contains("b", result);
        Assert.Contains("c", result);
    }

    [Fact]
    public void RemovePropertiesFromJsonRemovesSpecifiedProperties()
    {
        var json = "{\"name\":\"test\",\"value\":123,\"extra\":\"remove\"}";
        var propertiesToRemove = new[] { "extra" };
        
        var result = MudExJsonHelper.RemovePropertiesFromJson(json, propertiesToRemove);
        
        Assert.Contains("name", result);
        Assert.Contains("value", result);
        Assert.DoesNotContain("extra", result);
    }

    [Fact]
    public void RemovePropertiesFromJsonHandlesNestedProperties()
    {
        var json = "{\"outer\":{\"inner\":\"value\",\"keep\":\"this\"}}";
        var propertiesToRemove = new[] { "outer.inner" };
        
        var result = MudExJsonHelper.RemovePropertiesFromJson(json, propertiesToRemove);
        
        Assert.Contains("keep", result);
        Assert.DoesNotContain("inner", result);
    }

    [Fact]
    public void RemovePropertiesFromJsonReturnsOriginalWhenNoPropertiesToRemove()
    {
        var json = "{\"name\":\"test\"}";
        
        var result = MudExJsonHelper.RemovePropertiesFromJson(json, new string[0]);
        
        Assert.Equal(json, result);
    }

    [Fact]
    public void RemovePropertiesFromJsonReturnsOriginalWhenNullProperties()
    {
        var json = "{\"name\":\"test\"}";
        
        var result = MudExJsonHelper.RemovePropertiesFromJson(json, null);
        
        Assert.Equal(json, result);
    }

    [Fact]
    public void RemovePropertiesFromJsonHandlesEmptyJson()
    {
        var result = MudExJsonHelper.RemovePropertiesFromJson("", new[] { "prop" });
        
        Assert.Equal("", result);
    }

    [Fact]
    public void SimplifyMudColorInJsonRemovesColorWrapperObjects()
    {
        var json = "{\"Color\":{\"Value\":\"#FF0000\"}}";
        
        var result = MudExJsonHelper.SimplifyMudColorInJson(json);
        
        Assert.Contains("Color", result);
        Assert.Contains("#FF0000", result);
    }
}
