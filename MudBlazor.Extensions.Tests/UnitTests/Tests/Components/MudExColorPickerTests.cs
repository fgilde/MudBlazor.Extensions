using Microsoft.AspNetCore.Components.Web;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class MudExColorPickerTests
{
    // InitialColor reached MudColorPicker through reflected private methods that MudBlazor 9 made static with other
    // signatures, so the picker silently started at its default color
    [Fact]
    public async Task InitialColor_IsShownWhenThereIsNoValue()
    {
        await using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = context.Render<MudExColorPicker>(p => p
            .Add(c => c.PickerVariant, PickerVariant.Static)
            .Add(c => c.InitialColor, new MudColor("#00ff00")));

        Assert.Contains("rgb(0,255,0)", cut.Find(".mud-picker-color-overlay").GetAttribute("style")?.Replace(" ", ""));
        Assert.Null(cut.Instance.Value);
    }

    // With DelayValueChangeToPickerClose the change is reported on close. It reported the Value parameter, which
    // MudBlazor's ParameterState does not write back, so the parent got its own old color again
    [Fact]
    public async Task DelayedValueChange_ReportsThePickedColorOnClose()
    {
        await using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var reported = new List<MudColor>();
        var popovers = context.Render<MudPopoverProvider>();

        var cut = context.Render<MudExColorPicker>(p => p
            .Add(c => c.Value, new MudColor("#ff0000"))
            .Add(c => c.ValueChanged, c => reported.Add(c)));

        for (var round = 0; round < 2; round++)
        {
            await cut.InvokeAsync(() => cut.Instance.OpenAsync());
            // the innermost spectrum layer takes the pointer
            var spectrum = popovers.FindAll(".mud-picker-color-overlay[id]").Single();
            await cut.InvokeAsync(() => spectrum.PointerDown(new PointerEventArgs { OffsetX = 200 - round * 150, OffsetY = 20 + round * 100 }));
            Assert.Equal(round, reported.Count); // nothing reported while open
            await cut.InvokeAsync(() => cut.Instance.CloseAsync());
        }

        Assert.Equal(2, reported.Count);
        Assert.NotEqual(new MudColor("#ff0000"), reported[0]);
        Assert.NotEqual(reported[0], reported[1]);
    }
}
