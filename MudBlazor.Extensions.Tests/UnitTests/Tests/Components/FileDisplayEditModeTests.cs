using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using System.Reflection;
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

    private IRenderedComponent<MudPopoverProvider> _provider;

    private IRenderedComponent<MudExFileDisplay> Create(string fileName, string content, Action<ComponentParameterCollectionBuilder<MudExFileDisplay>> configure = null)
    {
        var context = new TestContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        _provider = context.Render<MudPopoverProvider>();
        var display = context.Render<MudExFileDisplay>(p =>
        {
            p.Add(c => c.FileName, fileName)
                .Add(c => c.ContentType, "text/plain")
                .Add(c => c.ContentStream, new MemoryStream(Encoding.UTF8.GetBytes(content)))
                .Add(c => c.ShowFileName, true);
            configure?.Invoke(p);
        });
        display.InvokeAsync(() => display.FindComponent<MoveContent>().Instance.ElementFoundChanged(false)).GetAwaiter().GetResult();
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
        return _provider.Markup;
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
        var possible = (List<IMudExFileDisplay>)typeof(MudExFileDisplay).GetField("_possibleRenderControls", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cut.Instance);
        var viewer = possible.First(c => c is not IMudExFileEditor);
        await cut.InvokeAsync(() => typeof(MudExFileDisplay).GetMethod("RenderWith", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(cut.Instance, [viewer]));
        await cut.InvokeAsync(() => typeof(Microsoft.AspNetCore.Components.ComponentBase).GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(cut.Instance, null));
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
}
