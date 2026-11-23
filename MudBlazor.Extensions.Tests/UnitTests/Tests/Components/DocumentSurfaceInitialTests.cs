using Bunit;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentSurfaceInitialTests : DocumentSurfaceTestBase
{
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
    public async Task Initial_TypingInASentChunk_IsAcceptedWithoutRestart()
    {
        var document = Numbered(1200);
        var id = document.Blocks[0].Id;
        var cut = Surface(null!);
        SurfaceResult? echo = null;
        var pauses = 0;
        cut.Instance.Pause = async () =>
        {
            if (++pauses == 2)
                echo = await cut.Instance.OnSyncAsync(new SurfaceSync(3, [new SurfaceTyped(id, "0abc", 4, null)], Sel(id, 4), false), null);
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(4, Patches().Count));
        await cut.InvokeAsync(() => Task.Delay(20));
        var patches = Patches().Select(Assert.Single).ToList();
        Assert.Equal([1, 2, 3, 5], patches.Select(p => p.Seq));
        Assert.Equal([1, 2], patches.Where(p => p.Reset).Select(p => p.Seq));
        Assert.Equal("0abc", Text(document));
        Assert.Equal((4, "0abc"), (Assert.Single(echo!.Patches).Seq, Upsert(echo, id).Block.Text));
        Assert.Equal(document.Blocks.Select(b => b.Id), Dom(patches));
    }

    [Fact]
    public async Task Initial_TwentyTypingBatches_DoNotDelayTheSend()
    {
        var document = Numbered(1200);
        var id = document.Blocks[0].Id;
        var cut = Surface(null!);
        var pauses = 0;
        cut.Instance.Pause = async () =>
        {
            if (++pauses != 1)
                return;
            var seen = 2;
            for (var i = 1; i <= 20; i++)
            {
                var text = "0" + new string('a', i);
                var result = await cut.Instance.OnSyncAsync(new SurfaceSync(seen, [new SurfaceTyped(id, text, text.Length, null)], Sel(id, text.Length), false), null);
                seen = result.Patches.Max(p => p.Seq);
            }
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(4, Patches().Count));
        await cut.InvokeAsync(() => Task.Delay(20));
        Assert.Equal(4, Patches().Count);
        Assert.Equal(2, pauses);
        Assert.Equal([1, 2], Patches().Select(Assert.Single).Where(p => p.Reset).Select(p => p.Seq));
        Assert.Equal("0" + new string('a', 20), Text(document));
    }

    [Fact]
    public async Task Initial_SplitDuringTheSend_RestartsAndKeepsTheTyping()
    {
        var document = Numbered(1200);
        var first = document.Blocks[0].Id;
        var tenth = document.Blocks[10].Id;
        var cut = Surface(null!);
        var pauses = 0;
        cut.Instance.Pause = async () =>
        {
            if (++pauses == 1)
            {
                await cut.Instance.OnSyncAsync(new SurfaceSync(2, [new SurfaceTyped(first, "0abc", 4, null)], Sel(first, 4), false), null);
                document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[5].Id, 1)));
            }
            else if (pauses == 2)
                await cut.Instance.OnSyncAsync(new SurfaceSync(3, [new SurfaceTyped(tenth, "10x", 3, null)], Sel(tenth, 3), false), null);
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(document.Blocks.Select(b => b.Id), Dom(Patches().Select(Assert.Single))));
        await cut.InvokeAsync(() => Task.Delay(20));
        var patches = Patches().Select(Assert.Single).ToList();
        Assert.Equal(patches.Select(p => p.Seq).Order(), patches.Select(p => p.Seq));
        var resets = patches.Where(p => p.Reset).ToList();
        Assert.Equal(3, resets.Count);
        Assert.Equal("0abc", resets[^1].Upsert.Single(u => u.Block.Id == first).Block.Text);
        Assert.Equal(("0abc", "10x"), (Text(document), ((Paragraph)document.FindBlock(tenth)).Text));
        Assert.Equal(1201, document.Blocks.Count);
    }

    [Fact]
    public async Task Dispose_StopsTheInitialSend()
    {
        var cut = Surface(null!);
        var pauses = 0;
        cut.Instance.Pause = async () =>
        {
            pauses++;
            await cut.Instance.DisposeAsync();
        };

        cut.Render(p => p.Add(c => c.Document, Numbered(1200)));

        cut.WaitForAssertion(() => Assert.Equal(2, Patches().Count));
        await cut.InvokeAsync(() => Task.Delay(20));
        Assert.Equal((2, 1), (Patches().Count, pauses));
    }

    [Fact]
    public void Initial_Restart_FlushesThePendingTypingIntoTheRestartReset()
    {
        var document = Numbered(1200);
        var first = document.Blocks[0].Id;
        var cut = Surface(null!);
        _context.JSInterop.Setup<SurfaceSync>("suspend", _ => true).SetResult(new SurfaceSync(2, [new SurfaceTyped(first, "0abc", 4, null)], Sel(first, 4), false));
        var pauses = 0;
        cut.Instance.Pause = () =>
        {
            if (++pauses == 1)
                document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[5].Id, 1)));
            return Task.CompletedTask;
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(document.Blocks.Select(b => b.Id), Dom(Patches().Select(Assert.Single))));
        Assert.Equal("0abc", Text(document));
        var resets = Patches().Select(Assert.Single).Where(p => p.Reset).ToList();
        Assert.Equal(3, resets.Count);
        Assert.Equal("0abc", resets[^1].Upsert.Single(u => u.Block.Id == first).Block.Text);
    }

    [Fact]
    public void Initial_MediaRegistrationThrows_TheChunkIsStillSent()
    {
        const string part = "word/media/image1.png";
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new InlineImage(part, 9525, 9525)] }]);
        document.PutMedia(part, [1, 2, 3], "image/png");
        _context.JSInterop.SetupVoid("registerMedia", _ => true).SetException(new ArgumentException("broken"));

        Surface(document);

        Assert.Equal([1], Patches().Select(p => Assert.Single(p).Seq));
    }

    [Theory]
    [InlineData("styles")]
    [InlineData("sections")]
    public void Initial_StylesOrSectionsChangeDuringTheSend_RendersTheDocumentOnce(string change)
    {
        var document = Sectioned(1200);
        var cut = Surface(null!);
        var pauses = 0;
        cut.Instance.Pause = () =>
        {
            if (++pauses != 1)
                return Task.CompletedTask;
            if (change == "styles")
                document.Execute(new ApplyParagraphStyleCommand([document.Blocks[3].Id], "Heading1"));
            else
                document.Execute(new SetPageSetupCommand(document.Sections[0].Id, document.Sections[0].Page with { MarginLeft = 2000 }));
            return Task.CompletedTask;
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(document.Blocks.Select(b => b.Id), Dom(Patches().Select(Assert.Single))));
        var patches = Patches().Select(Assert.Single).ToList();
        Assert.Equal(3, patches.Count(p => p.Reset));
        Assert.InRange(patches.Sum(p => p.Upsert.Count), 1700, 1710);
    }

    [Fact]
    public void Initial_HeaderOnlyStructureChangeDuringTheSend_DoesNotRestart()
    {
        var document = Sectioned(1200);
        document.Execute(new AddHeaderFooterCommand(document.Sections[0].Id, HeaderFooterKind.Header, HeaderFooterType.Default));
        var header = document.HeadersFooters[0].Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(header, 0), document.PositionOf(header, 0)), "head"));
        var cut = Surface(null!);
        var pauses = 0;
        cut.Instance.Pause = () =>
        {
            if (++pauses == 1)
                document.Execute(new SplitParagraphCommand(document.PositionOf(header, 2)));
            return Task.CompletedTask;
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(document.Blocks.Select(b => b.Id), Dom(Patches().Select(Assert.Single))));
        Assert.Equal(2, Patches().Select(Assert.Single).Count(p => p.Reset));
        Assert.Equal(2, pauses);
    }

    [Fact]
    public async Task Initial_HostEditThenRestart_StaleTypingIntoTheEditedParagraphIsRejected()
    {
        var document = Numbered(1200);
        var edited = document.Blocks[3].Id;
        var cut = Surface(null!);
        SurfaceResult? late = null;
        var pauses = 0;
        cut.Instance.Pause = async () =>
        {
            if (++pauses == 1)
            {
                document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(edited, 0), document.PositionOf(edited, 0)), "host"));
                document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[5].Id, 1)));
            }
            else if (pauses == 2)
                late = await cut.Instance.OnSyncAsync(new SurfaceSync(2, [new SurfaceTyped(edited, "3typed", 6, null)], Sel(edited, 6), false), null);
        };

        cut.Render(p => p.Add(c => c.Document, document));

        cut.WaitForAssertion(() => Assert.Equal(document.Blocks.Select(b => b.Id), Dom(Patches().Select(Assert.Single))));
        await cut.InvokeAsync(() => Task.Delay(20));
        Assert.Equal("host3", ((Paragraph)document.FindBlock(edited)).Text);
        Assert.Equal("host3", Upsert(late!, edited).Block.Text);
    }

    private static TextDocument Numbered(int count) =>
        TextDocument.FromBlocks(Enumerable.Range(0, count).Select(i => new Paragraph { Inlines = [new TextRun(i.ToString(System.Globalization.CultureInfo.InvariantCulture))] }));

    private static TextDocument Sectioned(int count)
    {
        var document = Numbered(count);
        document.SectionList.Add(new Section { Id = "s1", LastBlockId = document.Blocks[^1].Id });
        return document;
    }

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
