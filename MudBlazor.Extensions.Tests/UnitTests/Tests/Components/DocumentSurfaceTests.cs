using Bunit;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public abstract class DocumentSurfaceTestBase : IAsyncLifetime
{
    protected static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);
    protected readonly BunitContext _context = new();
    protected int _seen = 1;

    protected DocumentSurfaceTestBase()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
        _context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    protected sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    protected static TextDocument Doc(params string[] texts) => TextDocument.FromBlocks(texts.Select(t => new Paragraph { Inlines = t.Length == 0 ? [] : [new TextRun(t)] }));

    protected static string Text(TextDocument document, int index = 0) => ((Paragraph)document.Blocks[index]).Text;

    protected static SurfaceSelection Sel(string id, int offset) => new(new SurfacePoint(id, offset), new SurfacePoint(id, offset));

    protected IRenderedComponent<MudExDocumentSurface> Surface(TextDocument document, Action<ComponentParameterCollectionBuilder<MudExDocumentSurface>>? configure = null)
    {
        var cut = _context.Render<MudExDocumentSurface>(p =>
        {
            p.Add(c => c.Document, document);
            configure?.Invoke(p);
        });
        cut.Instance.Clock = new FixedClock(T0);
        cut.WaitForAssertion(() => Assert.NotEmpty(Patches()));
        return cut;
    }

    protected List<IReadOnlyList<SurfacePatch>> Patches() =>
        _context.JSInterop.Invocations.Where(i => i.Identifier == "patch").Select(i => (IReadOnlyList<SurfacePatch>)i.Arguments[0]!).ToList();

    protected SurfaceResult Seen(SurfaceResult result)
    {
        _seen = result.Patches.Select(p => p.Seq).DefaultIfEmpty(_seen).Max();
        return result;
    }

    protected async Task<SurfaceResult> Sync(IRenderedComponent<MudExDocumentSurface> cut, SurfaceSync sync)
    {
        SurfaceResult result = null!;
        await cut.InvokeAsync(async () => result = await cut.Instance.OnSyncAsync(sync, null));
        return Seen(result);
    }

    protected Task<SurfaceResult> Type(IRenderedComponent<MudExDocumentSurface> cut, string id, string text, bool endTyping = false) =>
        Sync(cut, new SurfaceSync(_seen, [new SurfaceTyped(id, text, text.Length, null)], Sel(id, text.Length), endTyping));

    protected async Task<SurfaceResult> Command(IRenderedComponent<MudExDocumentSurface> cut, SurfaceSync sync, string name, string? text = null)
    {
        SurfaceResult result = null!;
        await cut.InvokeAsync(async () => result = await cut.Instance.OnCommandAsync(sync, null, name, text));
        return Seen(result);
    }

    protected SurfaceSync Idle(string id, int offset) => new(_seen, [], Sel(id, offset), false);

    protected static SurfaceUpsert Upsert(SurfaceResult result, string id) => result.Patches.SelectMany(p => p.Upsert).Last(u => u.Block.Id == id);
}

public class DocumentSurfaceTests : DocumentSurfaceTestBase
{
    [Fact]
    public async Task Sync_AppliesTyping_ReturnsEchoPatchWithSeq()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        Assert.Equal(1, Assert.Single(Assert.Single(Patches())).Seq);

        var result = await Sync(cut, new SurfaceSync(1, [new SurfaceTyped(id, "Hallo", 2, null)], Sel(id, 2), false));

        Assert.Equal("Hallo", Text(document));
        var patch = Assert.Single(result.Patches);
        Assert.Equal(2, patch.Seq);
        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal((id, "Hallo"), (upsert.Block.Id, upsert.Block.Text));
        Assert.Null(result.Selection);
        Assert.Equal(document.PositionOf(id, 2), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Sync_EntryFromOldDom_IsRejectedAndParagraphResent()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        await cut.InvokeAsync(() => document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 5)), "Host")));
        Assert.Equal(2, Assert.Single(Patches()[1]).Seq);

        var result = await Sync(cut, new SurfaceSync(1, [new SurfaceTyped(id, "HelloX", 6, null)], Sel(id, 6), false));

        Assert.Equal("Host", Text(document));
        Assert.Equal(3, Assert.Single(result.Patches).Seq);
        Assert.Equal("Host", Upsert(result, id).Block.Text);
        Assert.Equal(document.PositionOf(id, 4), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Sync_UnknownParagraph_Ignored()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        var result = await Sync(cut, new SurfaceSync(1, [new SurfaceTyped("nope", "x", 1, null), new SurfaceTyped(null, "y", 0, null), new SurfaceTyped(id, null, 0, null), null!,
            new SurfaceTyped(id, "Hello!", 6, null)], Sel("nope", 1), false));

        Assert.Equal("Hello!", Text(document));
        Assert.DoesNotContain(result.Patches.SelectMany(p => p.Upsert), u => u.Block.Id == "nope");
        Assert.Equal(document.PositionOf(id, 0), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Sync_ReadOnly_IgnoresTyping()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document, p => p.Add(c => c.ReadOnly, true));

        var result = await Type(cut, id, "Hallo");

        Assert.Equal("Hello", Text(document));
        Assert.False(document.Undo.CanUndo);
        Assert.Equal("Hello", Upsert(result, id).Block.Text);
    }

    [Fact]
    public async Task Sync_EndTyping_StartsANewUndoStep()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        await Type(cut, id, "Hello ", endTyping: true);
        await Type(cut, id, "Hello w");
        await Type(cut, id, "Hello wo");
        await Command(cut, Idle(id, 8), "undo");

        Assert.Equal("Hello ", Text(document));
    }

    [Fact]
    public async Task Sync_FilteredCharacters_AreRenderedAgain()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        var result = await Sync(cut, new SurfaceSync(1, [new SurfaceTyped(id, "Hel\nlo", 4, null)], Sel(id, 4), false));

        Assert.Equal("Hello", Text(document));
        Assert.Equal("Hello", Upsert(result, id).Block.Text);
        Assert.True(Upsert(result, id).InPlace);
    }

    [Fact]
    public async Task Sync_DuplicateParagraph_LastEntryApplied()
    {
        var document = Doc("Hello", "World");
        var (first, second) = (document.Blocks[0].Id, document.Blocks[1].Id);
        var cut = Surface(document);

        await Sync(cut, new SurfaceSync(1, [new SurfaceTyped(first, "Hxllo", 2, null), new SurfaceTyped(second, "World!", null, null), new SurfaceTyped(first, "Hallo", 2, null)],
            Sel(first, 2), false));

        Assert.Equal(["Hallo", "World!"], [Text(document, 0), Text(document, 1)]);
    }

    [Fact]
    public async Task Sync_OffsetsOutOfRange_AreClamped()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        await Sync(cut, new SurfaceSync(int.MinValue, [new SurfaceTyped(id, "x", 99, null)], new SurfaceSelection(new SurfacePoint(id, -5), new SurfacePoint(id, 99)), false));
        await Sync(cut, new SurfaceSync(int.MaxValue, [new SurfaceTyped(id, "Hello!", -3, null)], new SurfaceSelection(new SurfacePoint(id, -5), new SurfacePoint(id, 99)), false));

        Assert.Equal("Hello!", Text(document));
        Assert.Equal(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 6)), cut.Instance.Selection);
    }

    [Fact]
    public async Task Sync_OverCaps_IsRejectedAndTheReportedParagraphsAreResent()
    {
        var document = Doc("Hello", "World");
        var (id, other) = (document.Blocks[0].Id, document.Blocks[1].Id);
        var cut = Surface(document);
        var many = Enumerable.Range(0, 10_001).Select(i => new SurfaceTyped(i == 0 ? other : id, "Hallo", 2, null)).ToList();
        var huge = new SurfaceTyped(id, new string('x', 8 * 1024 * 1024 + 1), 0, null);

        var tooMany = await Sync(cut, new SurfaceSync(1, many, Sel(id, 3), false));
        var tooLong = await Command(cut, new SurfaceSync(1, [huge, new SurfaceTyped("nope", "x", 0, null)], Sel(id, 3), false), "split");

        Assert.Equal([other, id], Assert.Single(tooMany.Patches).Upsert.Select(u => u.Block.Id));
        Assert.Equal([id], Assert.Single(tooLong.Patches).Upsert.Select(u => u.Block.Id));
        Assert.All(new[] { tooMany, tooLong }, r => Assert.Equal((null, null), (r.Selection, r.CaretFormat)));
        Assert.Equal(["Hello", "World"], document.Blocks.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal(document.PositionOf(id, 0), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Sync_HeaderParagraph_IsIgnored()
    {
        var document = TextDocument.Create();
        document.Execute(new AddHeaderFooterCommand(document.Sections[0].Id, HeaderFooterKind.Header, HeaderFooterType.Default));
        var header = document.HeadersFooters[0].Blocks[0].Id;
        var cut = Surface(document);

        var result = await Sync(cut, new SurfaceSync(int.MaxValue, [new SurfaceTyped(header, "head", 4, null)], Sel(header, 0), false));

        Assert.Equal("", ((Paragraph)document.HeadersFooters[0].Blocks[0]).Text);
        Assert.Empty(result.Patches);
        Assert.Equal(document.Blocks[0].Id, cut.Instance.Selection.End.BlockId);
    }

    [Fact]
    public async Task Sync_FromStream_IsRead_AndBadJsonIsRejected()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        var json = $$"""{"seen":1,"typed":[{"p":"{{id}}","t":"Hallo","c":2,"f":null}],"selection":null,"endTyping":false}""";

        SurfaceResult good = null!, bad = null!;
        await cut.InvokeAsync(async () => good = await cut.Instance.OnSyncAsync(null!, new TestStreamReference(json)));
        await cut.InvokeAsync(async () => bad = await cut.Instance.OnSyncAsync(null!, new TestStreamReference("{not json")));

        Assert.Equal("Hallo", Text(document));
        Assert.Single(good.Patches);
        Assert.Empty(bad.Patches);
    }

    [Fact]
    public async Task Command_FlushesTypingBeforeTheCommand()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        var result = await Command(cut, new SurfaceSync(1, [new SurfaceTyped(id, "HelloX", 6, null)], Sel(id, 6), false), "split");

        Assert.Equal(["HelloX", ""], document.Blocks.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal(new SurfacePoint(document.Blocks[1].Id, 0), result.Selection.Focus);
        Assert.Equal(new SurfacePoint(document.Blocks[1].Id, 0), result.Selection.Anchor);
        Assert.Equal(document.PositionOf(document.Blocks[1].Id, 0), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Command_UndoAfterBatches_RestoresTextAndCaret()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        await Type(cut, id, "Hello a");
        await Type(cut, id, "Hello ab");
        await Type(cut, id, "Hello abc");
        await Command(cut, Idle(id, 9), "split");
        var second = document.Blocks[1].Id;
        await Type(cut, second, "d");
        await Type(cut, second, "de");

        var first = await Command(cut, Idle(second, 2), "undo");
        Assert.Equal(["Hello abc", ""], document.Blocks.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal(new SurfacePoint(second, 0), first.Selection.Focus);

        var split = await Command(cut, Idle(second, 0), "undo");
        Assert.Equal(["Hello abc"], document.Blocks.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal(new SurfacePoint(id, 9), split.Selection.Focus);

        var typing = await Command(cut, Idle(id, 9), "undo");
        Assert.Equal(["Hello"], document.Blocks.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal(Sel(id, 5), typing.Selection);
        Assert.Equal(document.PositionOf(id, 5), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Command_UndoWithStaleSelection_SendsNoSelectionOrClampsIt()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        var range = new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 1));
        await cut.InvokeAsync(() => document.Execute(new ReplaceTextCommand(range, "J"), new DocumentRange(new DocumentPosition(id, [], 99), new DocumentPosition(id, [], 99))));
        await cut.InvokeAsync(() => document.Execute(new ReplaceTextCommand(range, "M"), new DocumentRange(new DocumentPosition("gone", [], 1), new DocumentPosition("gone", [], 1))));
        _seen = 3;

        var unknown = await Command(cut, Idle(id, 2), "undo");
        var clamped = await Command(cut, Idle(id, 2), "undo");

        Assert.Equal("Hello", Text(document));
        Assert.Null(unknown.Selection);
        Assert.Equal(Sel(id, 5), clamped.Selection);
        Assert.Equal(document.PositionOf(id, 5), cut.Instance.Selection.End);
    }

    [Fact]
    public async Task Command_ReadOnly_OnlyShortcutsPass()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 5), document.PositionOf(id, 5)), "!"));
        var keys = new List<string>();
        var cut = Surface(document, p => p.Add(c => c.ReadOnly, true).Add(c => c.Shortcuts, ["Ctrl+F"]).Add(c => c.ShortcutPressed, k =>
        {
            keys.Add(k);
            return Task.CompletedTask;
        }));

        await Command(cut, Idle(id, 2), "split");
        await Command(cut, Idle(id, 2), "undo");
        await Command(cut, Idle(id, 2), "shortcut", "Ctrl+F");
        await Command(cut, Idle(id, 2), "shortcut", "Ctrl+Q");

        Assert.Equal(["Hello!"], document.Blocks.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal(["Ctrl+F"], keys);
    }

    [Fact]
    public async Task Command_TextOverLimit_IsIgnored()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        await Command(cut, new SurfaceSync(1, [], new SurfaceSelection(new SurfacePoint(id, 0), new SurfacePoint(id, 5)), false), "replace", new string('x', 8001));
        await Command(cut, Idle(id, 5), "replace", "!");

        Assert.Equal("Hello!", Text(document));
    }

    [Fact]
    public async Task RunAsync_AppliesPendingTypingBeforeTheCommand()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(new SurfaceSync(1, [new SurfaceTyped(id, "HelloX", 6, null)], Sel(id, 6), false));

        await cut.InvokeAsync(() => cut.Instance.RunAsync(r =>
            document.Execute(new SetRunFormatCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 6)), f => f with { Bold = true }, "Bold"), r)));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("HelloX", paragraph.Text);
        Assert.All(paragraph.Inlines.OfType<TextRun>(), r => Assert.True(document.EffectiveRunFormat(paragraph, r).Bold));
        var result = (SurfaceResult)Assert.Single(_context.JSInterop.Invocations["resume"]).Arguments[0]!;
        Assert.Equal([2, 3], result.Patches.Select(p => p.Seq));
        Assert.Contains("font-weight:bold", Upsert(result, id).Block.Runs[0].Css);
    }

    [Fact]
    public async Task RunAsync_FailingAction_IsResumedReportedAndRethrown()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var errors = new List<Exception>();
        var cut = Surface(document, p => p.Add(c => c.OnError, e => errors.Add(e)));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => cut.InvokeAsync(() => cut.Instance.RunAsync(r =>
        {
            document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 5), document.PositionOf(id, 5)), "!"), r);
            throw new InvalidOperationException("boom");
        })));

        Assert.Same(thrown, Assert.Single(errors));
        var result = (SurfaceResult)Assert.Single(_context.JSInterop.Invocations["resume"]).Arguments[0]!;
        Assert.Equal("Hello!", Upsert(result, id).Block.Text);
    }

    [Fact]
    public async Task HostChange_OutsideAnOperation_IsPushed()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);

        await cut.InvokeAsync(() => document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 5), document.PositionOf(id, 5)), "!")));

        _context.JSInterop.VerifyInvoke("patch", 2);
        var patch = Assert.Single(Patches()[1]);
        Assert.Equal((2, "Hello!"), (patch.Seq, patch.Upsert.Single().Block.Text));
    }

    [Fact]
    public async Task CaretFormat_TokenAppliesToTheNextTypedText()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(Idle(id, 5));

        await cut.InvokeAsync(() => cut.Instance.RunAsync(_ => cut.Instance.SetCaretFormat(new RunFormat { Bold = true })));
        var format = ((SurfaceResult)_context.JSInterop.Invocations["resume"].Single().Arguments[0]!).CaretFormat;
        Assert.Contains("font-weight:bold", format.Css);
        Assert.True(cut.Instance.CaretFormat.Bold);

        var result = await Sync(cut, new SurfaceSync(_seen, [new SurfaceTyped(id, "HelloX", 6, format.Token)], Sel(id, 6), false));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Equal("HelloX", paragraph.Text);
        Assert.Equal([false, true], paragraph.Inlines.OfType<TextRun>().Select(r => document.EffectiveRunFormat(paragraph, r).Bold == true));
        Assert.Null(result.CaretFormat);
        Assert.Null(cut.Instance.CaretFormat);
    }

    [Fact]
    public async Task CaretFormat_StaysUntilTheSelectionMoves_AndUnknownTokensAreIgnored()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var cut = Surface(document);
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(Idle(id, 5));
        await cut.InvokeAsync(() => cut.Instance.RunAsync(_ => cut.Instance.SetCaretFormat(new RunFormat { Italic = true })));
        var token = ((SurfaceResult)_context.JSInterop.Invocations["resume"].Single().Arguments[0]!).CaretFormat.Token;

        var same = await Sync(cut, Idle(id, 5));
        Assert.Equal(token, same.CaretFormat.Token);
        await Sync(cut, new SurfaceSync(_seen, [new SurfaceTyped(id, "HelloX", 6, "forged")], Sel(id, 6), false));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.All(paragraph.Inlines.OfType<TextRun>(), r => Assert.NotEqual(true, document.EffectiveRunFormat(paragraph, r).Italic));
    }

    [Fact]
    public async Task SelectionChanged_IsRaisedWhenTheSelectionMoves()
    {
        var document = Doc("Hello");
        var id = document.Blocks[0].Id;
        var selections = new List<DocumentRange>();
        var cut = Surface(document, p => p.Add(c => c.SelectionChanged, r => selections.Add(r)));

        await Sync(cut, Idle(id, 0));
        await Sync(cut, Idle(id, 3));

        Assert.Equal([new DocumentRange(document.PositionOf(id, 3), document.PositionOf(id, 3))], selections);
    }

    [Fact]
    public async Task Media_IsRegisteredBeforeThePatchThatNeedsIt()
    {
        const string part = "word/media/image1.png";
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new InlineImage(part, 9525, 9525)] }]);
        document.PutMedia(part, [1, 2, 3], "image/png");
        var cut = Surface(document);

        var calls = _context.JSInterop.Invocations.Select(i => i.Identifier).Where(i => i is "registerMedia" or "patch").ToList();
        Assert.Equal(["registerMedia", "patch"], calls);
        var media = _context.JSInterop.Invocations["registerMedia"].Single().Arguments;
        Assert.Equal((part, "image/png"), ((string)media[0]!, (string)media[2]!));
        Assert.Equal(new byte[] { 1, 2, 3 }, (byte[])media[1]!);

        await cut.InvokeAsync(() => document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(document.Blocks[0].Id, 1), document.PositionOf(document.Blocks[0].Id, 1)), "x")));
        _context.JSInterop.VerifyInvoke("registerMedia", 1);
    }

    [Fact]
    public async Task Parameters_NewDocumentZoomAndReadOnly_ReachTheBrowser()
    {
        var document = Doc("Hello");
        var cut = Surface(document);
        var other = Doc("Other");

        cut.Render(p => p.Add(c => c.Document, other).Add(c => c.Zoom, 1.5).Add(c => c.ReadOnly, true));
        await cut.InvokeAsync(() => document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(document.Blocks[0].Id, 0), document.PositionOf(document.Blocks[0].Id, 0)), "x")));

        var reset = Assert.Single(Patches()[1]);
        Assert.True(reset.Reset);
        Assert.Equal((2, "Other"), (reset.Seq, reset.Upsert.Single().Block.Text));
        Assert.Equal(2, Patches().Count);
        Assert.Equal(1.5, _context.JSInterop.Invocations["setZoom"].Single().Arguments[0]);
        Assert.Equal(true, _context.JSInterop.Invocations["setReadOnly"].Single().Arguments[0]);
        Assert.Equal(other.PositionOf(other.Blocks[0].Id, 0), cut.Instance.Selection.End);
    }

    [Fact]
    public void Initial_LargeDocument_IsSentChunkByChunk_WithAPauseBetween()
    {
        var cut = Surface(null!);
        var pauses = new List<int>();
        cut.Instance.Pause = () =>
        {
            pauses.Add(Patches().Count);
            return Task.CompletedTask;
        };

        cut.Render(p => p.Add(c => c.Document, Numbered(1200)));

        cut.WaitForAssertion(() => Assert.Equal(4, Patches().Count));
        var patches = Patches().Skip(1).Select(Assert.Single).ToList();
        Assert.Equal([2, 3, 4], patches.Select(p => p.Seq));
        Assert.Equal([true, false, false], patches.Select(p => p.Reset));
        Assert.Equal([500, 500, 200], patches.Select(p => p.Upsert.Count));
        Assert.Equal([2, 3], pauses);
    }

    [Fact]
    public async Task Initial_HostChangeBetweenChunks_RestartsWithAFreshReset()
    {
        var document = Numbered(1200);
        var cut = Surface(null!);
        var changed = false;
        cut.Instance.Pause = () =>
        {
            if (!changed)
            {
                changed = true;
                document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[0].Id, 1)));
            }

            return Task.CompletedTask;
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(document.Blocks.Select(b => b.Id), Dom(Patches().Select(Assert.Single))));
        var patches = Patches().Select(Assert.Single).ToList();
        Assert.Equal(Enumerable.Range(1, patches.Count), patches.Select(p => p.Seq));
        Assert.Equal([1, 2, 4], patches.Where(p => p.Reset).Select(p => p.Seq));
        Assert.Equal(6, patches.Count);
        var id = document.Blocks[0].Id;
        var stale = await Sync(cut, new SurfaceSync(3, [new SurfaceTyped(id, "stale", 5, null)], Sel(id, 5), false));
        Assert.Empty(stale.Patches);
        Assert.Equal("0", Text(document));
        await Sync(cut, new SurfaceSync(6, [new SurfaceTyped(id, "0x", 2, null)], Sel(id, 2), false));
        Assert.Equal("0x", Text(document));
    }

    private static TextDocument Numbered(int count) =>
        TextDocument.FromBlocks(Enumerable.Range(0, count).Select(i => new Paragraph { Inlines = [new TextRun(i.ToString(System.Globalization.CultureInfo.InvariantCulture))] }));

    private static List<string> Dom(IEnumerable<SurfacePatch> patches)
    {
        var dom = new List<string>();
        foreach (var patch in patches)
        {
            if (patch.Reset) dom.Clear();
            dom.RemoveAll(patch.Remove.Contains);
            foreach (var upsert in patch.Upsert)
            {
                if (upsert.InPlace && dom.Contains(upsert.Block.Id)) continue;
                dom.Remove(upsert.Block.Id);
                dom.Insert(upsert.After == null ? 0 : dom.IndexOf(upsert.After) + 1, upsert.Block.Id);
            }
        }

        return dom;
    }
}
