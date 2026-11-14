using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;
using MudBlazor.Extensions.Options;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>The office editors open their dialogs with ShowExAsync and the editor's DialogOptions parameter.</summary>
public class OfficeEditorDialogOptionsTests : IAsyncLifetime
{
    private readonly BunitContext _context = new();
    private IRenderedComponent<MudDialogProvider> _dialogs;
    private CultureInfo _culture;

    public Task InitializeAsync()
    {
        _culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _context.Render<MudPopoverProvider>();
        _dialogs = _context.Render<MudDialogProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        CultureInfo.CurrentCulture = _culture;
        await _context.DisposeAsync();
    }

    [Fact]
    public void Unset_options_are_the_default_dialog_options_small_with_close_button()
    {
        var options = DialogOptionsEx.EditorDialogOptions(null);

        Assert.NotSame(DialogOptionsEx.DefaultDialogOptions, options);
        Assert.Equal(MaxWidth.Small, options.MaxWidth);
        Assert.True(options.FullWidth);
        Assert.True(options.CloseButton);
        Assert.Equal(DialogOptionsEx.DefaultDialogOptions.Position, options.Position);
        Assert.Equal(DialogOptionsEx.DefaultDialogOptions.Animations, options.Animations);
        Assert.Equal(MaxWidth.ExtraSmall, DialogOptionsEx.DefaultDialogOptions.MaxWidth);
    }

    [Fact]
    public void Set_options_are_used_as_they_are()
    {
        var options = new DialogOptionsEx { MaxWidth = MaxWidth.Large };

        Assert.Same(options, DialogOptionsEx.EditorDialogOptions(options));
    }

    [Theory]
    [InlineData(null, "mud-dialog-width-sm")]
    [InlineData(MaxWidth.Large, "mud-dialog-width-lg")]
    public void Spreadsheet_GoTo_dialog_uses_the_DialogOptions_parameter(MaxWidth? maxWidth, string expected)
    {
        var options = maxWidth is { } w ? new DialogOptionsEx { MaxWidth = w, FullWidth = true } : null;
        var cut = _context.Render<MudExSpreadsheetEditor>(p => p.Add(x => x.Workbook, Workbook.Create()).Add(x => x.DialogOptions, options));
        var grid = cut.FindComponent<MudExSpreadsheetGrid>();

        grid.Find(".mud-ex-spreadsheet-grid").KeyDown(new KeyboardEventArgs { Key = "F5" });

        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-ssgoto-dialog")));
        Assert.Contains(expected, _dialogs.Find(".mud-ex-ssgoto-dialog").ClassList);
    }

    [Theory]
    [InlineData(null, "mud-dialog-width-sm")]
    [InlineData(MaxWidth.Medium, "mud-dialog-width-md")]
    public async Task Slides_FindReplace_dialog_uses_the_DialogOptions_parameter(MaxWidth? maxWidth, string expected)
    {
        var options = maxWidth is { } w ? new DialogOptionsEx { MaxWidth = w, FullWidth = true } : null;
        var cut = _context.Render<MudExSlideEditor>(p => p.Add(x => x.Presentation, MudBlazor.Extensions.Office.Presentation.Presentation.Create()).Add(x => x.DialogOptions, options));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-slide-canvas")));

        await cut.Find(".mud-ex-slide-canvas").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "h", CtrlKey = true });

        _dialogs.WaitForAssertion(() => Assert.NotEmpty(_dialogs.FindAll(".mud-ex-slidefind-dialog")));
        Assert.Contains(expected, _dialogs.Find(".mud-ex-slidefind-dialog").ClassList);
    }
}
