using MudBlazor.Extensions.Helper;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

public class ExtensionsTests
{
    [Theory]
    [InlineData(DialogPosition.TopLeft, new[] { "top", "left" })]
    [InlineData(DialogPosition.TopCenter, new[] { "top", "center" })]
    [InlineData(DialogPosition.TopRight, new[] { "top", "right" })]
    [InlineData(DialogPosition.CenterLeft, new[] { "center", "left" })]
    [InlineData(DialogPosition.Center, new[] { "center" })]
    [InlineData(DialogPosition.CenterRight, new[] { "center", "right" })]
    [InlineData(DialogPosition.BottomLeft, new[] { "bottom", "left" })]
    [InlineData(DialogPosition.BottomCenter, new[] { "bottom", "center" })]
    [InlineData(DialogPosition.BottomRight, new[] { "bottom", "right" })]
    public void GetPositionNamesReturnsCorrectNames(DialogPosition position, string[] expected)
    {
        DialogPosition? nullablePosition = position;
        var result = nullablePosition.GetPositionNames();
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPositionNamesReturnsEmptyForNull()
    {
        DialogPosition? position = null;
        var result = position.GetPositionNames();
        
        Assert.Empty(result);
    }

    [Fact]
    public void GetPositionNamesSwitchesPositionsWhenRequested()
    {
        DialogPosition? position = DialogPosition.TopLeft;
        var result = position.GetPositionNames(switchPositions: true);
        
        Assert.Contains("down", result);
        Assert.Contains("right", result);
        Assert.DoesNotContain("top", result);
        Assert.DoesNotContain("left", result);
    }

    [Fact]
    public void TryLocalizeReturnsLocalizedTextWhenLocalizerProvided()
    {
        var mockLocalizer = new Mock<IStringLocalizer>();
        var localizedString = new LocalizedString("TestKey", "Localized Value", false);
        mockLocalizer.Setup(l => l["TestKey"]).Returns(localizedString);
        
        var result = mockLocalizer.Object.TryLocalize("TestKey");
        
        Assert.Equal("Localized Value", result);
    }

    [Fact]
    public void TryLocalizeReturnsOriginalTextWhenLocalizerIsNull()
    {
        IStringLocalizer localizer = null;
        
        var result = localizer.TryLocalize("TestKey");
        
        Assert.Equal("TestKey", result);
    }

    [Fact]
    public void TryLocalizeReturnsNullForNullText()
    {
        var mockLocalizer = new Mock<IStringLocalizer>();
        
        var result = mockLocalizer.Object.TryLocalize(null);
        
        Assert.Null(result);
    }

    [Fact]
    public void TryLocalizeHandlesArgumentsCorrectly()
    {
        var mockLocalizer = new Mock<IStringLocalizer>();
        var localizedString = new LocalizedString("TestKey", "Hello World", false);
        mockLocalizer.Setup(l => l["TestKey", It.IsAny<object[]>()]).Returns(localizedString);
        
        var result = mockLocalizer.Object.TryLocalize("TestKey", "World");
        
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void IsLocalizedReturnsTrueWhenResourceFound()
    {
        var mockLocalizer = new Mock<IStringLocalizer>();
        var localizedString = new LocalizedString("TestKey", "Localized Value", resourceNotFound: false);
        mockLocalizer.Setup(l => l["TestKey"]).Returns(localizedString);
        
        var result = mockLocalizer.Object.IsLocalized("TestKey");
        
        Assert.True(result);
    }

    [Fact]
    public void IsLocalizedReturnsFalseWhenResourceNotFound()
    {
        var mockLocalizer = new Mock<IStringLocalizer>();
        var localizedString = new LocalizedString("TestKey", "TestKey", resourceNotFound: true);
        mockLocalizer.Setup(l => l["TestKey"]).Returns(localizedString);
        
        var result = mockLocalizer.Object.IsLocalized("TestKey");
        
        Assert.False(result);
    }

    [Fact]
    public void IsLocalizedReturnsFalseForNullLocalizer()
    {
        IStringLocalizer localizer = null;
        
        var result = localizer.IsLocalized("TestKey");
        
        Assert.False(result);
    }

    [Fact]
    public void IsLocalizedReturnsFalseForNullText()
    {
        var mockLocalizer = new Mock<IStringLocalizer>();
        
        var result = mockLocalizer.Object.IsLocalized(null);
        
        Assert.False(result);
    }

    [Fact]
    public void ToHtmlGeneratesCorrectHtmlTag()
    {
        var data = ("div", new Dictionary<string, object> { { "id", "test" } });
        
        var result = data.ToHtml();
        
        Assert.Contains("<div", result);
        Assert.Contains("id=\"test\"", result);
        Assert.Contains("</div>", result);
    }

    [Fact]
    public void ToHtmlAddsStyleAttribute()
    {
        var data = ("div", new Dictionary<string, object>());
        
        var result = data.ToHtml(style: "color: red;");
        
        Assert.Contains("color: red", result);
    }

    [Fact]
    public void ToHtmlAddsClassAttribute()
    {
        var data = ("div", new Dictionary<string, object>());
        
        var result = data.ToHtml(cls: "test-class");
        
        Assert.Contains("class=", result);
        Assert.Contains("test-class", result);
    }

    [Fact]
    public void ToHtmlCombinesExistingStyleWithNewStyle()
    {
        var data = ("div", new Dictionary<string, object> { { "style", "margin: 10px;" } });
        
        var result = data.ToHtml(style: "color: blue;");
        
        Assert.Contains("style=", result);
        Assert.Contains("margin: 10px;", result);
        Assert.Contains("color: blue;", result);
    }
}
