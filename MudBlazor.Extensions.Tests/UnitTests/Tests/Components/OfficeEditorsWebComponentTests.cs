using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class OfficeEditorsWebComponentTests
{
    public static TheoryData<Type, string> Editors => new()
    {
        { typeof(MudExSpreadsheetEditor), "mudex-spreadsheet-editor" },
        { typeof(MudExSlideEditor), "mudex-slide-editor" },
        { typeof(MudExNotebookEditor), "mudex-notebook-editor" }
    };

    public static TheoryData<Type> EditorTypes => new(typeof(MudExSpreadsheetEditor), typeof(MudExSlideEditor), typeof(MudExNotebookEditor));

    private static BunitContext NewContext()
    {
        var context = new BunitContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        context.Render<MudPopoverProvider>();
        context.Render<MudDialogProvider>();
        return context;
    }

    private static IRenderedComponent<DynamicComponent> Render(BunitContext context, Type editor) =>
        context.Render<DynamicComponent>(p => p.Add(c => c.Type, editor).Add(c => c.Parameters, new Dictionary<string, object>
        {
            ["ShowRibbon"] = true,
            ["RibbonCollapsed"] = false,
            ["RibbonMode"] = MudExRibbonMode.Simplified,
            ["ReadOnly"] = false
        }));

    [Theory]
    [MemberData(nameof(Editors))]
    public void Editor_IsRegistrableAsCustomElement(Type editor, string tag) =>
        Assert.Contains(MudExWebComponents.GetRegistrableComponents(editor.Assembly), c => c.Type == editor && c.Tag == tag);

    [Theory]
    [MemberData(nameof(EditorTypes))]
    public async Task Editor_RendersFromParametersWithoutDocument(Type editor)
    {
        await using var context = NewContext();
        var cut = Render(context, editor);

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-ribbon")), TimeSpan.FromSeconds(10));
    }

    [Theory]
    [MemberData(nameof(EditorTypes))]
    public async Task Editor_RemovedWhileLoading_DoesNotThrow(Type editor)
    {
        await using var context = NewContext();
        var cut = Render(context, editor);

        var exception = await Record.ExceptionAsync(async () => await context.DisposeAsync());
        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(EditorTypes))]
    public async Task Editor_RemovedAfterLoading_DoesNotThrow(Type editor)
    {
        await using var context = NewContext();
        var cut = Render(context, editor);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-ribbon")), TimeSpan.FromSeconds(10));

        var exception = await Record.ExceptionAsync(async () => await context.DisposeAsync());
        Assert.Null(exception);
    }
}
