using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorTests : IAsyncLifetime
{
    private const string DocxType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string Notice = "This document contains tracked changes. They are shown accepted; paragraphs you edit are saved without their marks.";
    private static readonly string Fixture = Path.Combine(AppContext.BaseDirectory, "TestData", "Document", "document-all-features.docx");
    private readonly List<BunitContext> _contexts = [];
    private BunitContext _context = null!;
    private IRenderedComponent<MudDialogProvider> _dialogs = null!;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();
    }

    private sealed record Infos(string FileName, string? ContentType, byte[] Bytes) : IMudExFileDisplayInfos
    {
        public string? Url => null;
        public Stream ContentStream { get; } = new MemoryStream(Bytes);
    }

    private static byte[] Bytes(TextDocument document, DocumentFileFormat format = DocumentFileFormat.Docx)
    {
        var stream = new MemoryStream();
        document.Save(stream, format);
        return stream.ToArray();
    }

    private IRenderedComponent<MudExDocumentEditor> Render(Action<ComponentParameterCollectionBuilder<MudExDocumentEditor>> configure)
    {
        _context = new BunitContext();
        _contexts.Add(_context);
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        _context.Render<MudPopoverProvider>();
        _dialogs = _context.Render<MudDialogProvider>();
        var cut = _context.Render(configure);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".mud-ex-doc-sheet")));
        return cut;
    }

    private IRenderedComponent<MudExDocumentEditor> Render(TextDocument document, bool readOnly = false)
        => Render(p => p.Add(x => x.TextDocument, document).Add(x => x.ReadOnly, readOnly));

    private void Answer(string button) =>
        _dialogs.WaitForAssertion(() => _dialogs.FindAll("button").Single(b => b.TextContent.Trim() == button).Click());

    private static TextDocument Merged()
    {
        var document = DocumentSurfaceRenderTests.WithTable();
        document.Execute(new MergeTableCellsCommand(document.Blocks.OfType<Table>().Single().Id, 0, 0, 0, 1));
        return document;
    }

    private static Task Edit(IRenderedComponent<MudExDocumentEditor> cut, string text) => cut.InvokeAsync(() =>
    {
        var document = cut.Instance.CurrentDocument;
        var at = document.PositionOf(document.Blocks[^1].Id, ((Paragraph)document.Blocks[^1]).Length);
        document.Execute(new ReplaceTextCommand(new DocumentRange(at, at), text));
    });

    [Fact]
    public void Standalone_WithoutFile_EditsABlankDocument()
    {
        var cut = Render(_ => { });

        var document = cut.Instance.CurrentDocument;
        Assert.Equal(0, Assert.IsType<Paragraph>(Assert.Single(document.Blocks)).Length);
        Assert.Equal("Document editor", cut.Instance.Name);
        Assert.Equal(10, cut.Instance.RenderPriority);
        Assert.Equal(1, cut.Instance.Zoom);
        Assert.Equal("false", cut.Find(".mud-ex-doc-editor").GetAttribute("data-readonly"));
        Assert.Contains(_context.JSInterop.Invocations, i => i.Identifier == "patch");
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task TextDocumentParameter_EditedInPlace_RaisesTextDocumentChanged()
    {
        TextDocument? raised = null;
        var document = TextDocument.Create();
        var cut = Render(p => p.Add(x => x.TextDocument, document).Add(x => x.TextDocumentChanged, d => raised = d));

        await Edit(cut, "Hello");

        cut.WaitForAssertion(() => Assert.Same(document, raised));
        Assert.Same(document, cut.Instance.CurrentDocument);
        Assert.Equal("Hello", ((Paragraph)document.Blocks[0]).Text);
        Assert.True(cut.Instance.IsDirty);
    }

    [Fact]
    public void LoadsTheAllFeaturesFixtureFromFileDisplayInfos()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("features.docx", DocxType, File.ReadAllBytes(Fixture))));

        cut.WaitForAssertion(() => Assert.True(cut.Instance.CurrentDocument?.Blocks.Count > 10));
        Assert.Equal("features.docx", cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Save_Unchanged_PartsByteIdentical()
    {
        var source = File.ReadAllBytes(Fixture);
        byte[]? saved = null;
        var cut = Render(p => p
            .Add(x => x.FileDisplayInfos, new Infos("features.docx", DocxType, source))
            .Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
            {
                saved = ((MemoryStream)r.Content.Stream).ToArray();
                r.Handled = true;
            }));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.CurrentDocument?.Blocks.Count > 10));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        var before = OpcAssert.Parts(source);
        var after = OpcAssert.Parts(saved!);
        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        foreach (var (part, bytes) in before) Assert.True(bytes.AsSpan().SequenceEqual(after[part]), part);
    }

    [Fact]
    public async Task Save_FlushesPendingTyping()
    {
        byte[]? saved = null;
        var cut = Render(p => p.Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
        {
            saved = ((MemoryStream)r.Content.Stream).ToArray();
            r.Handled = true;
        }));
        var id = cut.Instance.CurrentDocument.Blocks[0].Id;
        var caret = new SurfaceSelection(new SurfacePoint(id, 1), new SurfacePoint(id, 1));
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(new SurfaceSync(1000, [new SurfaceTyped(id, "X", 1, null)], caret, false));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        Assert.Equal("X", ((Paragraph)TextDocument.Load(new MemoryStream(saved!)).Blocks[0]).Text);
        Assert.Equal("Document1.docx", cut.Instance.FileName);
    }

    [Fact]
    public void SaveFormats_OriginalFirst_DocAndOdtSaveAsDocx()
    {
        string[] Extensions(IRenderedComponent<MudExDocumentEditor> editor) => editor.Instance.SaveFormats.Select(f => f.Extension).ToArray();
        IRenderedComponent<MudExDocumentEditor> Load(string name, byte[] bytes, string? type = null)
        {
            var editor = Render(p => p.Add(x => x.FileDisplayInfos, new Infos(name, type, bytes)));
            editor.WaitForAssertion(() => Assert.NotNull(editor.Instance.CurrentDocument));
            return editor;
        }

        var folder = Path.Combine(AppContext.BaseDirectory, "TestData", "Document");
        Assert.Equal(["rtf", "docx", "html", "md", "txt"], Extensions(Load("letter.rtf", Bytes(TextDocument.Create(), DocumentFileFormat.Rtf))));
        Assert.Equal(["htm", "docx", "rtf", "md", "txt"], Extensions(Load("page.htm", Bytes(TextDocument.Create(), DocumentFileFormat.Html))));
        Assert.Equal(["docx", "rtf", "html", "md", "txt"], Extensions(Load("WordDokument.doc", File.ReadAllBytes(Path.Combine(folder, "WordDokument.doc")))));
        Assert.Equal(["docx", "rtf", "html", "md", "txt"], Extensions(Load("sample.odt", File.ReadAllBytes(Path.Combine(folder, "sample.odt")))));

        var macros = TextDocument.Create();
        macros.HasMacros = true;
        Assert.Equal(["docm", "rtf", "html", "md", "txt"], Extensions(Render(macros)));
    }

    [Fact]
    public async Task SaveAsMarkdown_WithMergedCells_WarnsOnce_DocxChoiceExportsDocx()
    {
        var requests = new List<(string FileName, string ContentType)>();
        var cut = Render(p => p.Add(x => x.TextDocument, Merged()).Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
        {
            requests.Add((r.Content.FileName, r.Content.ContentType));
            r.Handled = true;
        }));
        var markdown = cut.Instance.SaveFormats.Single(f => f.Extension == "md");
        var text = cut.Instance.SaveFormats.Single(f => f.Extension == "txt");

        var first = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(markdown));
        Answer("Keep Markdown");
        Assert.True(await first);
        var second = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(markdown));
        cut.WaitForAssertion(() => Assert.True(second.IsCompleted));
        Assert.True(await second);
        var docx = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(text));
        Answer("Save as .docx");
        Assert.True(await docx);

        Assert.Equal([("Document1.md", "text/markdown"), ("Document1.md", "text/markdown"), ("Document1.docx", DocxType)], requests);
    }

    [Fact]
    public async Task SaveAsMarkdown_WithMergedCells_Cancel_ReturnsFalseWithoutError()
    {
        var requested = false;
        var cut = Render(p => p.Add(x => x.TextDocument, Merged()).Add(x => x.SaveRequested, (MudExFileSaveRequest r) => requested = r.Handled = true));

        var save = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(cut.Instance.SaveFormats.Single(f => f.Extension == "md")));
        Answer("Cancel");

        Assert.False(await save);
        Assert.False(requested);
        _dialogs.WaitForAssertion(() => Assert.Empty(_dialogs.FindAll(".mud-dialog")));
    }

    [Fact]
    public void ReadOnly_ShowsOnlyTheViewTab()
    {
        var cut = Render(TextDocument.Create(), readOnly: true);

        cut.WaitForAssertion(() => Assert.Equal(["View"], cut.FindComponent<MudExRibbon>().FindAll("[role=tab]").Select(t => t.TextContent.Trim())));
        var ids = cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance.Id).ToList();
        Assert.DoesNotContain("wd-save", ids);
        Assert.DoesNotContain("wd-undo", ids);
        Assert.Contains("wd-zoom-100", ids);
        Assert.Equal("true", cut.Find(".mud-ex-doc-editor").GetAttribute("data-readonly"));
    }

    [Fact]
    public void RibbonColor_DefaultIsWordBlue()
    {
        Assert.Equal("#2B579A", new MudExDocumentEditor().RibbonColor.ToString());

        var cut = Render(TextDocument.Create());

        Assert.Equal("#2B579A", cut.FindComponent<MudExRibbon>().Instance.Color.ToString());
        Assert.StartsWith("--mud-ex-doc-accent:#2b579a;", cut.Find(".mud-ex-doc-editor").GetAttribute("style"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HostReplacesDocument_ResetsFileNameAndDirty()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("letter.docx", DocxType, Bytes(TextDocument.Create()))));
        cut.WaitForAssertion(() => Assert.Equal("letter.docx", cut.Instance.FileName));
        await Edit(cut, "changed");
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));
        var other = TextDocument.Create();

        cut.Render(p => p.Add(x => x.TextDocument, other));

        cut.WaitForAssertion(() => Assert.Same(other, cut.Instance.CurrentDocument));
        Assert.Null(cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task NewAsync_CreatesDocument1Docx()
    {
        var cut = Render(_ => { });
        var document = cut.Instance.CurrentDocument;

        Assert.True(await cut.InvokeAsync(() => cut.Instance.NewAsync()));

        cut.WaitForAssertion(() => Assert.NotSame(document, cut.Instance.CurrentDocument));
        Assert.Single(cut.Instance.CurrentDocument.Blocks);
        Assert.Equal("Document1.docx", cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task InvalidFile_ShowsErrorAndLoadFailed()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("data.bin", "application/octet-stream", [0, 1, 2, 3])));

        _dialogs.WaitForAssertion(() => Assert.Contains("Error", _dialogs.Markup));
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find(".mud-ex-doc-editor").GetAttribute("data-readonly")));
        Assert.False(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));
    }

    [Fact]
    public async Task StatusBar_CountsWords()
    {
        var cut = Render(DocumentSurfaceRenderTests.Created("one two", "three"));

        Assert.Equal("3 words", cut.Find(".mud-ex-doc-status-words").TextContent.Trim());

        await Edit(cut, " four\uFFFC ");

        cut.WaitForAssertion(() => Assert.Equal("4 words", cut.Find(".mud-ex-doc-status-words").TextContent.Trim()));
    }

    [Fact]
    public async Task TrackedChanges_NoticeShownOnceWhenEditing()
    {
        var document = TextDocument.Create();
        document.HasTrackedChanges = true;
        var cut = Render(document);
        Assert.DoesNotContain(Notice, _dialogs.Markup);

        await Edit(cut, "a");
        await Edit(cut, "b");

        _dialogs.WaitForAssertion(() => Assert.Contains(Notice, _dialogs.Markup));
        Assert.Single(_dialogs.FindAll(".mud-dialog"));
    }

    [Theory]
    [InlineData("a.docx", null, true)]
    [InlineData("a.dotx", null, true)]
    [InlineData("a.docm", null, true)]
    [InlineData("a.doc", null, true)]
    [InlineData("a.rtf", null, true)]
    [InlineData("a.odt", null, true)]
    [InlineData("download", "application/rtf", true)]
    [InlineData("a.html", "text/html", false)]
    [InlineData("a.md", null, false)]
    [InlineData("a.txt", "text/plain", false)]
    public async Task CanHandle_WordFormatsButNotWebOrText(string fileName, string? contentType, bool handled)
    {
        Assert.Equal(handled, await new MudExDocumentEditor().CanHandleFileAsync(new Infos(fileName, contentType, []), null!));
    }
}
