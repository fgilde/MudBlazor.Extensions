using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExFileTests
{
    [Fact]
    public void CanCreateFileDisplayMarkup()
    {
        var html = MudExFileDisplay.GetFileRenderInfos("123", "http://www.image.com/file.png", "Test.png", "image/png")
            .ToHtml(cls: "test-cls");
        Assert.Equal("<img data-id=\"123\" src=\"http://www.image.com/file.png\" loading=\"lazy\" alt=\"Test.png\" data-mimetype=\"image/png\" class=\"test-cls\"></img>", html);
    }
}