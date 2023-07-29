
using Bunit;
using Bunit.TestDoubles;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class MudExIconPickerTest
{
    [Fact]
    public void ShouldDisplaysIcons()
    {        
        using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        context.JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        
        var cut = context.RenderComponent<MudExIconPicker>();

        cut.Find("button.mud-button-root").Click();                
        Assert.NotEmpty(cut.Find(".mud-icon-root")?.ToMarkup() ?? string.Empty);
    }
}