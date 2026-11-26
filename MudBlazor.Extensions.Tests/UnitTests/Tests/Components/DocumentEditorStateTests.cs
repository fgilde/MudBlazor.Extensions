using System.IO.Compression;
using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorStateTests : DocumentEditorTestBase
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    private sealed class CountingEditor : MudExDocumentEditor
    {
        public int Renders { get; private set; }

        protected override void OnAfterRender(bool firstRender)
        {
            Renders++;
            base.OnAfterRender(firstRender);
        }
    }

    private static Task Click(IRenderedComponent<MudExDocumentEditor> cut, string id) =>
        cut.InvokeAsync(() => cut.FindComponents<MudExRibbonButton>().Select(b => b.Instance).Single(b => b.Id == id).ExecuteAsync());

    private static byte[] Docm()
    {
        using var stream = new MemoryStream();
        stream.Write(Bytes(TextDocument.Create()));
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Update, true))
        {
            var entry = zip.GetEntry("[Content_Types].xml")!;
            string xml;
            using (var reader = new StreamReader(entry.Open()))
                xml = reader.ReadToEnd();
            entry.Delete();
            using var writer = new StreamWriter(zip.CreateEntry("[Content_Types].xml").Open());
            writer.Write(xml.Replace(DocxType + ".main+xml", "application/vnd.ms-word.document.macroEnabled.main+xml"));
        }

        return stream.ToArray();
    }

    [Fact]
    public void BoundDocument_AssignedBack_KeepsTheLoadedFormat()
    {
        TextDocument? bound = null;
        var cut = Render(p => p
            .Add(x => x.FileDisplayInfos, new Infos("letter.rtf", null, Bytes(TextDocument.Create(), DocumentFileFormat.Rtf)))
            .Add(x => x.TextDocumentChanged, d => bound = d));
        cut.WaitForAssertion(() => Assert.NotNull(bound));

        cut.Render(p => p.Add(x => x.TextDocument, bound));

        Assert.Same(bound, cut.Instance.CurrentDocument);
        Assert.Equal("Rich Text Format", cut.Instance.SaveFormats[0].Name);
        Assert.Equal("letter.rtf", cut.Instance.FileName);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Typing_RendersOnlyOnStateChanges_AndRaisesTextDocumentChangedNever()
    {
        var replaced = 0;
        var edits = 0;
        var document = TextDocument.Create();
        var cut = RenderEditor<CountingEditor>(p => p.Add(x => x.TextDocument, document).Add(x => x.TextDocumentChanged, _ => replaced++).Add(x => x.DocumentEdited, _ => edits++));
        await cut.InvokeAsync(() => Task.Delay(50));
        var renders = cut.Instance.Renders;
        var id = document.Blocks[0].Id;

        for (var i = 1; i <= 20; i++)
        {
            var text = new string('a', i);
            await cut.InvokeAsync(() => document.ApplyTyping([new TypedParagraph(id, text)], T0));
        }

        cut.WaitForAssertion(() => Assert.Equal(20, edits));
        await cut.InvokeAsync(() => Task.Delay(50));
        Assert.InRange(cut.Instance.Renders - renders, 1, 2);
        Assert.Equal(0, replaced);
        Assert.True(cut.Instance.IsDirty);
        Assert.Equal("1 word", cut.Find(".mud-ex-doc-status-words").TextContent.Trim());
    }

    [Fact]
    public void WordCount_FollowsEveryKindOfChange()
    {
        var document = DocumentSurfaceRenderTests.Created("one two", "three");
        var count = new DocumentWordCount(document);
        document.Changed += (_, e) => count.Apply(e);
        void Check() => Assert.Equal(document.AllParagraphs().Sum(p => MudExDocumentEditor.CountWords(p.Text)), count.Total);
        var first = document.Blocks[0].Id;
        Assert.Equal(3, count.Total);

        document.ApplyTyping([new TypedParagraph(first, "one two four")], T0);
        Check();
        document.Execute(new InsertTableCommand(document.PositionOf(document.Blocks[^1].Id, 5), 2, 2));
        Check();
        var table = document.Blocks.OfType<Table>().Single();
        document.ApplyTyping([new TypedParagraph(table.Rows[1].Cells[1].Blocks[0].Id, "cell words here")], T0.AddSeconds(5));
        Check();
        document.Execute(new SplitParagraphCommand(document.PositionOf(first, 3)));
        Check();
        document.Execute(new MergeParagraphsCommand(first));
        Check();
        document.Execute(new DeleteTableCommand(table.Id));
        Check();
        while (document.Undo.CanUndo)
        {
            document.Undo.Undo();
            Check();
        }

        while (document.Undo.CanRedo)
        {
            document.Undo.Redo();
            Check();
        }
    }

    [Fact]
    public void WordCount_StaysRightAfterMergingTableCells()
    {
        var document = DocumentSurfaceRenderTests.WithTable();
        var count = new DocumentWordCount(document);
        document.Changed += (_, e) => count.Apply(e);
        void Check() => Assert.Equal(document.AllParagraphs().Sum(p => MudExDocumentEditor.CountWords(p.Text)), count.Total);
        document.ApplyTyping([new TypedParagraph(DocumentSurfaceRenderTests.Cell(document, 0, 0), "left words")], T0);
        document.ApplyTyping([new TypedParagraph(DocumentSurfaceRenderTests.Cell(document, 0, 1), "right side words")], T0.AddSeconds(5));
        Check();

        document.Execute(new MergeTableCellsCommand(document.Blocks.OfType<Table>().Single().Id, 0, 0, 0, 1));

        Check();
        Assert.Equal(6, count.Total);
        document.Undo.Undo();
        Check();
        document.Undo.Redo();
        Check();
    }

    [Fact]
    public async Task EditDuringSave_LeavesTheEditorDirty()
    {
        var release = new TaskCompletionSource();
        var saving = false;
        var edits = 0;
        var cut = Render(p => p.Add(x => x.TextDocument, TextDocument.Create()).Add(x => x.DocumentEdited, _ => edits++).Add(x => x.SaveRequested, async (MudExFileSaveRequest r) =>
        {
            saving = true;
            await release.Task;
            r.Handled = true;
        }));
        await Edit(cut, "a");
        cut.WaitForAssertion(() => Assert.True(cut.Instance.IsDirty));

        var save = cut.InvokeAsync(() => cut.Instance.SaveAsync());
        cut.WaitForAssertion(() => Assert.True(saving));
        await Edit(cut, "b");
        cut.WaitForAssertion(() => Assert.Equal(2, edits));
        release.SetResult();

        Assert.True(await save);
        Assert.True(cut.Instance.IsDirty);
    }

    [Fact]
    public void SaveCancelled_IsAnOperationCanceledException() =>
        Assert.IsAssignableFrom<OperationCanceledException>(new MudBlazor.Extensions.Components.Base.SaveCancelledException());

    [Fact]
    public async Task TrackedChangesNotice_DoesNotBlockSaving()
    {
        var document = DocumentSurfaceRenderTests.Created("x");
        document.HasTrackedChanges = true;
        var cut = Render(p => p.Add(x => x.TextDocument, document).Add(x => x.SaveRequested, (MudExFileSaveRequest r) => r.Handled = true));

        await Edit(cut, "a");
        _dialogs.WaitForAssertion(() => Assert.Contains(Notice, _dialogs.Markup));
        var save = cut.InvokeAsync(() => cut.Instance.SaveAsync());

        cut.WaitForAssertion(() => Assert.True(save.IsCompleted));
        Assert.True(await save);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public void EmptyFile_ShowsTheFileIsEmpty()
    {
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("empty.docx", DocxType, [])));

        _dialogs.WaitForAssertion(() => Assert.Contains("The file is empty.", _dialogs.Markup));
        Assert.Equal("true", cut.Find(".mud-ex-doc-editor").GetAttribute("data-readonly"));
    }

    [Fact]
    public async Task Export_WithoutDocumentOrWhenCancelled_Throws()
    {
        var failed = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("data.bin", "application/octet-stream", [0, 1, 2, 3])));
        failed.WaitForAssertion(() => Assert.Null(failed.Instance.CurrentDocument));
        var cut = Render(TextDocument.Create());

        await Assert.ThrowsAsync<InvalidOperationException>(() => failed.InvokeAsync(() => failed.Instance.ExportAsync()));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cut.InvokeAsync(() => cut.Instance.ExportAsync(null, new CancellationToken(true))));
    }

    [Fact]
    public async Task LossyKeep_RememberedOnlyAfterTheSaveSucceeded()
    {
        var cancel = true;
        var saved = 0;
        var cut = Render(p => p.Add(x => x.TextDocument, Merged()).Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
        {
            r.Cancel = cancel;
            r.Handled = !cancel;
            saved += cancel ? 0 : 1;
        }));
        var markdown = cut.Instance.SaveFormats.Single(f => f.Extension == "md");

        var first = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(markdown));
        Answer("Keep Markdown");
        Assert.False(await first);
        cancel = false;
        var second = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(markdown));
        Answer("Keep Markdown");
        Assert.True(await second);
        var third = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(markdown));

        cut.WaitForAssertion(() => Assert.True(third.IsCompleted));
        Assert.True(await third);
        Assert.Equal(2, saved);
    }

    [Fact]
    public async Task SecondDocument_ResetsTheRememberedChoices()
    {
        var cut = Render(p => p.Add(x => x.TextDocument, Merged()).Add(x => x.SaveRequested, (MudExFileSaveRequest r) => r.Handled = true));
        var markdown = cut.Instance.SaveFormats.Single(f => f.Extension == "md");
        var first = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(markdown));
        Answer("Keep Markdown");
        Assert.True(await first);

        cut.Render(p => p.Add(x => x.TextDocument, Merged()));
        var second = cut.InvokeAsync(() => cut.Instance.SaveCopyAsync(markdown));

        Answer("Keep Markdown");
        Assert.True(await second);
    }

    [Fact]
    public async Task ReplacedDocument_IsUnsubscribed()
    {
        var edits = 0;
        var old = TextDocument.Create();
        var cut = Render(p => p.Add(x => x.TextDocument, old).Add(x => x.DocumentEdited, _ => edits++));
        cut.Render(p => p.Add(x => x.TextDocument, TextDocument.Create()));

        await cut.InvokeAsync(() => old.Execute(new ReplaceTextCommand(new DocumentRange(old.PositionOf(old.Blocks[0].Id, 0), old.PositionOf(old.Blocks[0].Id, 0)), "x")));
        await cut.InvokeAsync(() => Task.Delay(50));

        Assert.Equal(0, edits);
        Assert.False(cut.Instance.IsDirty);
    }

    [Fact]
    public async Task Docm_KeepsTheMacroContentType()
    {
        MudExFileSaveRequest? request = null;
        byte[]? saved = null;
        var cut = Render(p => p.Add(x => x.FileDisplayInfos, new Infos("macro.docm", null, Docm())).Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
        {
            request = r;
            saved = ((MemoryStream)r.Content.Stream).ToArray();
            r.Handled = true;
        }));
        cut.WaitForAssertion(() => Assert.True(cut.Instance.CurrentDocument?.HasMacros));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.SaveAsync()));

        Assert.Equal(("macro.docm", "application/vnd.ms-word.document.macroEnabled.12"), (request!.Content.FileName, request.Content.ContentType));
        Assert.False(request.SaveAs);
        Assert.Contains("macroEnabled.main+xml", System.Text.Encoding.UTF8.GetString(OpcAssert.Parts(saved!)["/[Content_Types].xml"]));
    }

    [Fact]
    public async Task Zoom_ShowsTheCurrentValue_AndSharesOneClamp()
    {
        var cut = Render(TextDocument.Create());

        await Click(cut, "wd-zoom-in");

        Assert.Equal(1.1, cut.Instance.Zoom);
        var select = cut.FindComponent<MudExRibbonSelect<double>>().Instance;
        Assert.Contains(1.1, select.Items);
        Assert.Equal(1.1, select.Value);
        cut.Find(".mud-ex-doc-status-bar .mud-ex-sssb-slider").Change("900");
        Assert.Equal(5, cut.Instance.Zoom);
        await Click(cut, "wd-zoom-in");
        Assert.Equal(5, cut.Instance.Zoom);
        Assert.Equal((0.1, 1d), (MudExDocumentStatusBar.ClampZoom(0), MudExDocumentStatusBar.ClampZoom(double.NaN)));
    }

    [Fact]
    public async Task SaveShortcut_OnTheSurface_Saves()
    {
        var saved = 0;
        var cut = Render(p => p.Add(x => x.SaveRequested, (MudExFileSaveRequest r) =>
        {
            r.Handled = true;
            saved++;
        }));
        var surface = cut.FindComponent<MudExDocumentSurface>().Instance;

        Assert.Contains("Ctrl+S", surface.Shortcuts!);
        await cut.InvokeAsync(() => surface.ShortcutPressed("Ctrl+S"));

        Assert.Equal(1, saved);
    }
}
