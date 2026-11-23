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
