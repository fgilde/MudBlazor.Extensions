using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class FileDisplayEditModeTests : IAsyncLifetime
{
    private readonly List<TestContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private readonly Dictionary<IRenderedComponent<MudExFileDisplay>, IRenderedComponent<MudPopoverProvider>> _providers = [];
    private readonly Dictionary<IRenderedComponent<MudExFileDisplay>, IRenderedComponent<MudDialogProvider>> _dialogs = [];
    private readonly Dictionary<IRenderedComponent<MudExFileDisplay>, TestContext> _owners = [];

    private IRenderedComponent<MudExFileDisplay> Create(string fileName, string content, Action<ComponentParameterCollectionBuilder<MudExFileDisplay>> configure = null)
    {
        var context = new TestContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        var provider = context.Render<MudPopoverProvider>();
        var dialogs = context.Render<MudDialogProvider>();
        var display = context.Render<MudExFileDisplay>(p =>
        {
            p.Add(c => c.FileName, fileName)
                .Add(c => c.ContentType, "text/plain")
                .Add(c => c.ContentStream, new MemoryStream(Encoding.UTF8.GetBytes(content)))
                .Add(c => c.ShowFileName, true);
            configure?.Invoke(p);
        });
        display.InvokeAsync(() => display.FindComponent<MoveContent>().Instance.ElementFoundChanged(false)).GetAwaiter().GetResult();
        _providers[display] = provider;
        _dialogs[display] = dialogs;
        _owners[display] = context;
        return display;
    }

    private static MudExFileDisplayCode Editor(IRenderedComponent<MudExFileDisplay> cut) => cut.FindComponent<MudExFileDisplayCode>().Instance;

    [Fact]
    public void EditMenu_OnlyWhenAllowEditAndEditorAvailable()
    {
        var editable = Create("a.cs", "x", p => p.Add(c => c.AllowEdit, true));
        Assert.True(editable.Instance.CanEdit);
        Assert.Contains(">Edit<", RenderedMenu(editable));

        var image = Create("a.png", "x", p => p.Add(c => c.AllowEdit, true));
        Assert.False(image.Instance.CanEdit);
        Assert.DoesNotContain(">Edit<", RenderedMenu(image));

        var notAllowed = Create("a.cs", "x");
        Assert.DoesNotContain(">Edit<", RenderedMenu(notAllowed));
    }

    private string RenderedMenu(IRenderedComponent<MudExFileDisplay> cut)
    {
        cut.Find(".mud-menu button").Click();
        return _providers[cut].Markup;
    }

    private void RenderWithViewer(IRenderedComponent<MudExFileDisplay> cut)
    {
        RenderedMenu(cut);
        _providers[cut].FindAll(".mud-ex-file-display-menu-item-action")
            .First(i => i.TextContent.Contains("Render with") && !i.TextContent.Contains("native"))
            .Click();
    }

    [Fact]
    public async Task SwitchingToEditMode_KeepsEditorInstance_AndClearsReadOnly()
    {
        var cut = Create("a.cs", "x", p => p.Add(c => c.AllowEdit, true));
        var before = Editor(cut);
        Assert.True(before.ReadOnly);

        await cut.InvokeAsync(() => cut.Instance.SetEditModeAsync(true));

        Assert.Same(before, Editor(cut));
        Assert.False(Editor(cut).ReadOnly);
        Assert.True(cut.Instance.EditMode);
    }

    [Fact]
    public async Task SwitchingFromViewerToEditMode_SelectsEditor()
    {
        var cut = Create("a.json", "{\"a\":1}", p => p.Add(c => c.AllowEdit, true));
        RenderWithViewer(cut);
        Assert.Empty(cut.FindComponents<MudExFileDisplayCode>());

        await cut.InvokeAsync(() => cut.Instance.SetEditModeAsync(true));

        Assert.False(Editor(cut).ReadOnly);
    }

    [Fact]
    public void DirtyEditor_ShowsDirtyMarker_AndRaisesIsDirtyChanged()
    {
        var raised = new List<bool>();
        var cut = Create("a.cs", "x", p => p
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EditMode, true)
            .Add(c => c.IsDirtyChanged, EventCallback.Factory.Create<bool>(this, v => raised.Add(v))));
        Assert.Empty(cut.FindAll(".mud-ex-file-display-dirty"));

        cut.Find("textarea").Input("changed");

        Assert.True(cut.Instance.IsDirty);
        Assert.Equal([true], raised);
        Assert.Contains("●", cut.Find(".mud-ex-file-display-dirty").TextContent);
    }

    [Fact]
    public async Task SaveRequested_FromFileDisplay_ReachesHost()
    {
        MudExFileSaveRequest seen = null;
        var cut = Create("a.cs", "x", p => p
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EditMode, true)
            .Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, r => { seen = r; r.Handled = true; })));
        cut.Find("textarea").Input("changed");

        var saved = await cut.InvokeAsync(() => cut.Instance.SaveAsync());

        Assert.True(saved);
        Assert.NotNull(seen);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task NewNonEditableFile_LeavesEditMode()
    {
        var changes = new List<bool>();
        var cut = Create("a.cs", "x", p => p
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EditMode, true)
            .Add(c => c.EditModeChanged, EventCallback.Factory.Create<bool>(this, v => changes.Add(v))));
        Assert.Single(cut.FindComponents<MudExFileDisplayCode>());

        cut.Render(p => p
            .Add(c => c.FileName, "a.png")
            .Add(c => c.ContentStream, new MemoryStream([1, 2, 3])));

        Assert.False(cut.Instance.EditMode);
        Assert.Equal([false], changes);
        Assert.Empty(cut.FindComponents<MudExFileDisplayCode>());
    }
    [Fact]
    public void RenderWithViewer_WhenDirty_RaisesIsDirtyChangedFalse()
    {
        var raised = new List<bool>();
        var cut = Create("a.json", "{\"a\":1}", p => p
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EditMode, true)
            .Add(c => c.IsDirtyChanged, EventCallback.Factory.Create<bool>(this, v => raised.Add(v))));
        cut.Find("textarea").Input("changed");

        RenderWithViewer(cut);
        ClickDialogButton(cut, "Discard");

        cut.WaitForAssertion(() => Assert.False(cut.Instance.IsDirty));
        Assert.Equal([true, false], raised);
    }

    [Fact]
    public void RenderWithViewer_InEditMode_LeavesEditMode()
    {
        var changes = new List<bool>();
        var cut = Create("a.json", "{\"a\":1}", p => p
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EditMode, true)
            .Add(c => c.EditModeChanged, EventCallback.Factory.Create<bool>(this, v => changes.Add(v))));

        RenderWithViewer(cut);

        Assert.False(cut.Instance.EditMode);
        Assert.Equal([false], changes);
        Assert.Empty(cut.FindComponents<MudExFileDisplayCode>());

        cut.Render(p => p.Add(c => c.AllowEdit, true));

        Assert.Empty(cut.FindComponents<MudExFileDisplayCode>());
    }

    private void ClickClose(IRenderedComponent<MudExFileDisplay> cut)
    {
        RenderedMenu(cut);
        _providers[cut].FindAll(".mud-ex-file-display-menu-item-action").First(i => i.TextContent.Contains("Close")).Click();
    }

    private IRenderedComponent<MudExFileDisplay> CreateDirty(Action<ComponentParameterCollectionBuilder<MudExFileDisplay>> configure = null)
    {
        var cut = Create("a.cs", "x", p =>
        {
            p.Add(c => c.AllowEdit, true).Add(c => c.EditMode, true).Add(c => c.CanClose, true);
            configure?.Invoke(p);
        });
        cut.Find("textarea").Input("changed");
        return cut;
    }

    private void ClickDialogButton(IRenderedComponent<MudExFileDisplay> cut, string text)
    {
        cut.WaitForAssertion(() => Assert.Contains(_dialogs[cut].FindAll("button"), b => b.TextContent.Trim() == text));
        _dialogs[cut].FindAll("button").First(b => b.TextContent.Trim() == text).Click();
    }

    [Fact]
    public void Close_WhenDirty_AsksBeforeClosing()
    {
        var closed = 0;
        var cut = CreateDirty(p => p.Add(c => c.OnCloseClick, EventCallback.Factory.Create(this, () => closed++)));

        ClickClose(cut);

        Assert.Equal(0, closed);
        ClickDialogButton(cut, "Discard");
        cut.WaitForAssertion(() => Assert.Equal(1, closed));
    }

    [Fact]
    public void Close_WhenDirty_CancelKeepsFileOpen()
    {
        var closed = 0;
        var cut = CreateDirty(p => p.Add(c => c.OnCloseClick, EventCallback.Factory.Create(this, () => closed++)));

        ClickClose(cut);
        ClickDialogButton(cut, "Cancel");

        cut.WaitForAssertion(() => Assert.DoesNotContain(_dialogs[cut].FindAll("button"), b => b.TextContent.Trim() == "Discard"));
        Assert.Equal(0, closed);
        Assert.True(cut.Instance.IsDirty);
    }

    [Fact]
    public void Close_WhenDirty_SaveSavesThenCloses()
    {
        var closed = 0;
        var cut = CreateDirty(p => p
            .Add(c => c.OnCloseClick, EventCallback.Factory.Create(this, () => closed++))
            .Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, r => r.Handled = true)));

        ClickClose(cut);
        ClickDialogButton(cut, "Save");

        cut.WaitForAssertion(() => Assert.Equal(1, closed));
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public void Close_WhenClean_DoesNotAsk()
    {
        var closed = 0;
        var cut = Create("a.cs", "x", p => p.Add(c => c.CanClose, true).Add(c => c.OnCloseClick, EventCallback.Factory.Create(this, () => closed++)));

        ClickClose(cut);

        Assert.Equal(1, closed);
        Assert.Empty(_dialogs[cut].FindAll("button"));
    }

    [Fact]
    public void Close_WhenDirtyAndConfirmDisabled_DoesNotAsk()
    {
        var closed = 0;
        var cut = CreateDirty(p => p
            .Add(c => c.ConfirmDiscardChanges, false)
            .Add(c => c.OnCloseClick, EventCallback.Factory.Create(this, () => closed++)));

        ClickClose(cut);

        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task Dirty_CallsJsSetDirty()
    {
        var cut = CreateDirty(p => p.Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, r => r.Handled = true)));
        var interop = _owners[cut].JSInterop;
        Assert.Contains(interop.Invocations, i => i.Identifier == "setDirty" && true.Equals(i.Arguments[0]));

        await cut.InvokeAsync(() => cut.Instance.SaveAsync());

        Assert.Contains(interop.Invocations, i => i.Identifier == "setDirty" && false.Equals(i.Arguments[0]));
    }
}
