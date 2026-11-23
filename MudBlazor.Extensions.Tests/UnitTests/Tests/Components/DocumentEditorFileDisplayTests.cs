using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class DocumentEditorFileDisplayTests : IAsyncLifetime
{
    private const string DocxType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);
    private readonly List<BunitContext> _contexts = [];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private IRenderedComponent<MudExFileDisplay> Display(string fileName, string contentType, byte[] bytes, Action<ComponentParameterCollectionBuilder<MudExFileDisplay>> configure = null)
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

    private static byte[] Bytes(DocumentFileFormat format = DocumentFileFormat.Docx)
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 0)), "Shown"));
        var stream = new MemoryStream();
        document.Save(stream, format);
        return stream.ToArray();
    }

    private static byte[] TestData(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "TestData", "Document", name));

    [Fact]
    public void Docx_UsesDocumentEditor_ReadOnlyByDefault()
    {
        var display = Display("letter.docx", DocxType, Bytes(), p => p.Add(c => c.AllowEdit, true));

        display.WaitForAssertion(() => Assert.NotEmpty(display.FindComponents<MudExDocumentEditor>()));
        var editor = display.FindComponent<MudExDocumentEditor>().Instance;
        display.WaitForAssertion(() => Assert.Equal("Shown", editor.CurrentDocument?.AllParagraphs().First().Text));
        Assert.True(editor.ReadOnly);
        Assert.Empty(display.FindComponents<MudExFileDisplayDocument>());
    }

    [Fact]
    public async Task Docx_EditMode_FlipsReadOnly_WithoutReload()
    {
        var display = Display("letter.docx", DocxType, Bytes(), p => p.Add(c => c.AllowEdit, true));
        display.WaitForAssertion(() => Assert.NotNull(display.FindComponent<MudExDocumentEditor>().Instance.CurrentDocument));
        var editor = display.FindComponent<MudExDocumentEditor>().Instance;
        var document = editor.CurrentDocument;

        await display.InvokeAsync(() => display.Instance.SetEditModeAsync(true));

        display.WaitForAssertion(() => Assert.False(display.FindComponent<MudExDocumentEditor>().Instance.ReadOnly));
        Assert.Same(editor, display.FindComponent<MudExDocumentEditor>().Instance);
        Assert.Same(document, editor.CurrentDocument);
    }

    [Fact]
    public void Rtf_And_Doc_UseDocumentEditor()
    {
        var rtf = Display("note.rtf", "application/rtf", Bytes(DocumentFileFormat.Rtf));
        rtf.WaitForAssertion(() => Assert.NotEmpty(rtf.FindComponents<MudExDocumentEditor>()));
        Assert.Empty(rtf.FindComponents<MudExFileDisplayDocument>());

        var doc = Display("Rich.doc", "application/msword", TestData("Rich.doc"));
        doc.WaitForAssertion(() => Assert.NotEmpty(doc.FindComponents<MudExDocumentEditor>()));
        Assert.Empty(doc.FindComponents<MudExFileDisplayDocument>());
    }

    [Fact]
    public async Task Odt_ViewerStaysOpenDocument_EditOpensDocumentEditor_SavesAsDocx()
    {
        var display = Display("sample.odt", "application/vnd.oasis.opendocument.text", TestData("sample.odt"), p => p.Add(c => c.AllowEdit, true));

        display.WaitForAssertion(() => Assert.NotEmpty(display.FindComponents<MudExFileDisplayOpenDocument>()));
        Assert.Empty(display.FindComponents<MudExDocumentEditor>());
        Assert.True(display.Instance.CanEdit);

        await display.InvokeAsync(() => display.Instance.SetEditModeAsync(true));

        display.WaitForAssertion(() => Assert.NotEmpty(display.FindComponents<MudExDocumentEditor>()));
        var editor = display.FindComponent<MudExDocumentEditor>().Instance;
        Assert.False(editor.ReadOnly);
        Assert.Equal("docx", editor.SaveFormats[0].Extension);
    }

    [Fact]
    public async Task Html_Md_Txt_AreNotClaimed()
    {
        var editor = new MudExDocumentEditor();
        var html = Display("page.html", "text/html", Encoding.UTF8.GetBytes("<p>Hi</p>"));
        html.WaitForAssertion(() => Assert.NotEmpty(html.FindComponents<MudExFileDisplayCode>()));
        Assert.Empty(html.FindComponents<MudExDocumentEditor>());
        Assert.True(await new MudExFileDisplayHtml().CanHandleFileAsync(html.Instance, null));
        Assert.False(await editor.CanHandleFileAsync(html.Instance, null));

        var md = Display("readme.md", "text/markdown", Encoding.UTF8.GetBytes("# Hi"));
        md.WaitForAssertion(() => Assert.NotEmpty(md.FindComponents<MudExFileDisplayMarkdown>()));
        Assert.False(await editor.CanHandleFileAsync(md.Instance, null));

        var txt = Display("note.txt", "text/plain", Encoding.UTF8.GetBytes("Hi"), p => p.Add(c => c.AllowEdit, true));
        Assert.False(await editor.CanHandleFileAsync(txt.Instance, null));
        await txt.InvokeAsync(() => txt.Instance.SetEditModeAsync(true));
        Assert.Empty(txt.FindComponents<MudExDocumentEditor>());
    }

    [Fact]
    public async Task Dirty_PropagatesToTheDisplay_SaveRequestedHandled()
    {
        MudExFileSaveRequest seen = null;
        var display = Display("letter.docx", DocxType, Bytes(), p => p
            .Add(c => c.AllowEdit, true)
            .Add(c => c.EditMode, true)
            .Add(c => c.SaveRequested, EventCallback.Factory.Create<MudExFileSaveRequest>(this, r => { seen = r; r.Handled = true; })));
        display.WaitForAssertion(() => Assert.NotNull(display.FindComponent<MudExDocumentEditor>().Instance.CurrentDocument));
        var document = display.FindComponent<MudExDocumentEditor>().Instance.CurrentDocument;
        Assert.False(display.Instance.IsDirty);

        await display.InvokeAsync(() => document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "Changed")], T0));

        display.WaitForAssertion(() => Assert.True(display.Instance.IsDirty));
        Assert.True(await display.InvokeAsync(() => display.Instance.SaveAsync()));
        Assert.NotNull(seen);
        Assert.EndsWith(".docx", seen.Content.FileName);
        Assert.False(display.Instance.IsDirty);
    }
}
