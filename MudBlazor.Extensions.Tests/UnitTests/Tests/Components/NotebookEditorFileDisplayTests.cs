using System.Text;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookEditorFileDisplayTests : IAsyncLifetime
{
    private readonly List<BunitContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private IRenderedComponent<MudExFileDisplay> Display(string fileName, string contentType, byte[] bytes, Action<ComponentParameterCollectionBuilder<MudExFileDisplay>>? configure = null)
    {
        var context = new BunitContext();
        _contexts.Add(context);
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        context.Render<MudPopoverProvider>();
        context.Render<MudDialogProvider>();
        var display = context.Render<MudExFileDisplay>(p =>
        {
            p.Add(c => c.FileName, fileName).Add(c => c.ContentType, contentType).Add(c => c.ContentStream, new MemoryStream(bytes));
            configure?.Invoke(p);
        });
        display.InvokeAsync(() => display.FindComponent<MoveContent>().Instance.ElementFoundChanged(false)).GetAwaiter().GetResult();
        return display;
    }

    private static byte[] NotebookBytes()
    {
        var notebook = Notebook.Create();
        notebook.Execute(new RenamePageCommand(notebook.Sections[0].Pages[0], "Shown"));
        var stream = new MemoryStream();
        notebook.Save(stream);
        return stream.ToArray();
    }

    [Fact]
    public async Task Mxnb_InFileDisplay_UsesNotebookEditor_ReadOnlyByDefault()
    {
        var display = Display("notes.mxnb", "application/octet-stream", NotebookBytes(), p => p.Add(c => c.AllowEdit, true));

        display.WaitForAssertion(() => Assert.NotEmpty(display.FindComponents<MudExNotebookEditor>()));
        var editor = display.FindComponent<MudExNotebookEditor>().Instance;
        display.WaitForAssertion(() => Assert.Equal("Shown", editor.CurrentPage?.Title));
        Assert.True(editor.ReadOnly);
        Assert.Empty(display.FindComponents<MudExFileDisplayNotebook>());

        await display.InvokeAsync(() => display.Instance.SetEditModeAsync(true));

        display.WaitForAssertion(() => Assert.False(display.FindComponent<MudExNotebookEditor>().Instance.ReadOnly));
        Assert.Same(editor, display.FindComponent<MudExNotebookEditor>().Instance);
        Assert.NotEmpty(display.FindAll("input.mud-ex-notebook-title"));
    }

    [Fact]
    public async Task Ipynb_InFileDisplay_StillUsesJupyterViewer()
    {
        const string ipynb = """{"cells":[{"cell_type":"markdown","metadata":{},"source":["# Hello"]}],"metadata":{},"nbformat":4,"nbformat_minor":5}""";

        var display = Display("analysis.ipynb", "application/x-ipynb+json", Encoding.UTF8.GetBytes(ipynb));

        display.WaitForAssertion(() => Assert.NotEmpty(display.FindComponents<MudExFileDisplayNotebook>()));
        Assert.Empty(display.FindComponents<MudExNotebookEditor>());
        Assert.False(await new MudExNotebookEditor().CanHandleFileAsync(display.Instance, null!));
    }
}
