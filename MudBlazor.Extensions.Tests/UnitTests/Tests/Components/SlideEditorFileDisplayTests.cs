using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class SlideEditorFileDisplayTests : IAsyncLifetime
{
    private const string PptxType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    private readonly List<BunitContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private static byte[] SampleBytes => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Presentation", "sample.pptx"));

    private (IRenderedComponent<MudExFileDisplay> Display, IRenderedComponent<MudDialogProvider> Dialogs) Create(Action<ComponentParameterCollectionBuilder<MudExFileDisplay>> configure = null)
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        context.Render<MudPopoverProvider>();
        var dialogs = context.Render<MudDialogProvider>();
        var display = context.Render<MudExFileDisplay>(p =>
        {
            p.Add(c => c.FileName, "deck.pptx")
                .Add(c => c.ContentType, PptxType)
                .Add(c => c.ContentStream, new MemoryStream(SampleBytes));
            configure?.Invoke(p);
        });
        display.InvokeAsync(() => display.FindComponent<MoveContent>().Instance.ElementFoundChanged(false)).GetAwaiter().GetResult();
        display.WaitForAssertion(() => Assert.NotEmpty(display.FindComponents<MudExSlideEditor>()));
        return (display, dialogs);
    }

    [Fact]
    public void Pptx_InFileDisplay_UsesSlideEditor_ReadOnlyByDefault()
    {
        var (display, _) = Create();

        Assert.True(display.FindComponent<MudExSlideEditor>().Instance.ReadOnly);
    }

    [Fact]
    public void NoPptxPresentationReferenced()
    {
        var root = AppContext.BaseDirectory;
        while (root != null && !File.Exists(Path.Combine(root, "MudBlazor.Extensions.slnx")))
            root = Path.GetDirectoryName(root);
        Assert.NotNull(root);

        var offenders = Directory.EnumerateFiles(Path.Combine(root, "MudBlazor.Extensions"), "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".razor"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains("PptxPresentation") || File.ReadAllText(f).Contains("FileDisplayPresentation"))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public async Task Pptx_EditMode_TogglesReadOnlyWithoutReload()
    {
        var (display, _) = Create(p => p.Add(c => c.AllowEdit, true));
        var editor = display.FindComponent<MudExSlideEditor>().Instance;
        var presentation = editor.CurrentPresentation;
        Assert.True(editor.ReadOnly);

        await display.InvokeAsync(() => display.Instance.SetEditModeAsync(true));

        display.WaitForAssertion(() => Assert.False(display.FindComponent<MudExSlideEditor>().Instance.ReadOnly));
        Assert.Same(editor, display.FindComponent<MudExSlideEditor>().Instance);
        Assert.Same(presentation, editor.CurrentPresentation);

        await display.InvokeAsync(() => display.Instance.SetEditModeAsync(false));

        display.WaitForAssertion(() => Assert.True(display.FindComponent<MudExSlideEditor>().Instance.ReadOnly));
        Assert.Same(presentation, display.FindComponent<MudExSlideEditor>().Instance.CurrentPresentation);
    }

    [Fact]
    public async Task Pptx_DirtySwitchFile_AsksToSave()
    {
        var saves = 0;
        var (display, dialogs) = Create(p => p
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EditMode, true)
            .Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, r => { saves++; r.Handled = true; })));
        display.FindAll(".mud-ex-slide-panel-item")[1].Click();
        display.Find(".mud-ex-slide-notes textarea").Input("changed notes");
        display.Find(".mud-ex-slide-notes textarea").Blur();
        display.WaitForAssertion(() => Assert.True(display.Instance.IsDirty));

        var confirm = display.InvokeAsync(() => display.Instance.ConfirmDiscardChangesAsync());
        dialogs.WaitForAssertion(() => Assert.Contains(dialogs.FindAll("button"), b => b.TextContent.Trim() == "Save"));
        dialogs.FindAll("button").First(b => b.TextContent.Trim() == "Save").Click();

        Assert.True(await confirm);
        Assert.Equal(1, saves);
        Assert.False(display.Instance.IsDirty);
    }
}
