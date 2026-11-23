using System.Text;
using Bunit;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditorClipboardTests : DocumentEditorFormattingTestBase
{
    private const int Cap = 8 * 1024 * 1024;
    private const string Clip = "0123456789abcdef0123456789abcdef";
    private const string OtherClip = "fedcba9876543210fedcba9876543210";

    private sealed class Bytes(byte[] data) : IJSStreamReference
    {
        public long Length => data.Length;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => ValueTask.FromResult<Stream>(new MemoryStream(data));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static IJSStreamReference? Stream(string? text) => text == null ? null : new Bytes(Encoding.UTF8.GetBytes(text));

    private static SurfaceSync Sync(string a, int ao, string b, int bo) => new(Seen, [], new SurfaceSelection(new SurfacePoint(a, ao), new SurfacePoint(b, bo)), false);

    private static Task Copy(IRenderedComponent<MudExDocumentEditor> cut, SurfaceSync sync, string clip, bool cutOut = false) =>
        cut.InvokeAsync(() => Surface(cut).OnCopyAsync(sync, clip, cutOut));

    private static Task Paste(IRenderedComponent<MudExDocumentEditor> cut, SurfaceSync sync, string? html = null, string? text = null, byte[]? image = null, string? clip = null) =>
        cut.InvokeAsync(() => Surface(cut).OnPasteAsync(sync, Stream(html), Stream(text), image == null ? null : new Bytes(image), image == null ? null : "image/png", clip));

    private static string[] Texts(TextDocument document) => document.Blocks.OfType<Paragraph>().Select(p => p.Text).ToArray();

    [Fact]
    public async Task Paste_InternalClip_IsLossless()
    {
        var document = DocumentSurfaceRenderTests.Created("Title", "item", "tail", "end");
        document.Execute(new ApplyParagraphStyleCommand([document.Blocks[0].Id], "Heading1"));
        document.Execute(new ApplyListCommand([document.Blocks[1].Id], ListTemplate.Decimal));
        document.Undo.Clear();
        var cut = Render(document);
        var ids = document.Blocks.Select(b => b.Id).ToList();

        await Copy(cut, Sync(ids[0], 0, ids[2], 0), Clip);
        await Paste(cut, Sync(ids[3], 0, ids[3], 0), html: "<p>other</p>", text: "other", clip: Clip);

        Assert.Equal(["Title", "item", "tail", "Title", "item", "end"], Texts(document));
        var pasted = document.Blocks.Cast<Paragraph>().ToList();
        Assert.Equal("Heading1", pasted[3].Format.StyleId);
        Assert.Equal(pasted[1].Format.Numbering, pasted[4].Format.Numbering);
        Assert.Equal(["1.", "2."], new[] { pasted[1], pasted[4] }.Select(document.ListLabel));
        document.Undo.Undo();
        Assert.Equal(["Title", "item", "tail", "end"], Texts(document));
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public async Task Paste_HostileHtml_IsSanitised()
    {
        var document = DocumentSurfaceRenderTests.Created("");
        var cut = Render(document);
        var id = document.Blocks[0].Id;

        await Paste(cut, Sync(id, 0, id, 0), html: "<p onclick=x>a<script>alert(1)</script><a href=\"javascript:x\">b</a><a href=\"file:///c:/x\">c</a><img src=\"http://evil/x.png\"><style>p{background:url(http://evil)}</style></p>");

        Assert.Equal(["abc"], Texts(document));
        var inlines = document.AllParagraphs().SelectMany(p => p.Inlines).ToList();
        Assert.All(inlines, i => Assert.Null(i.Link));
        Assert.DoesNotContain(inlines, i => i is InlineImage);
        document.Undo.Undo();
        Assert.Equal([""], Texts(document));
    }

    [Fact]
    public async Task Paste_DeeplyNestedHtml_DoesNotCrash()
    {
        var document = DocumentSurfaceRenderTests.Created("");
        var cut = Render(document);
        var id = document.Blocks[0].Id;

        await Paste(cut, Sync(id, 0, id, 0), html: string.Concat(Enumerable.Repeat("<div>", 100_000)) + "x");

        Assert.True(Texts(document).SequenceEqual(["x"]) || Texts(document).SequenceEqual([""]));
    }

    [Fact]
    public async Task Paste_OverCap_RefusedNothingChanged()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        var id = document.Blocks[0].Id;
        var version = document.Version;

        await Paste(cut, Sync(id, 0, id, 0), text: new string('a', Cap + 1));

        _dialogs.WaitForAssertion(() => Assert.Contains("too large", _dialogs.Markup));
        Assert.Equal(version, document.Version);
        Assert.Equal(["Hello"], Texts(document));
    }

    [Fact]
    public async Task Paste_PlainTextWithCrLf_SplitsParagraphs()
    {
        var document = DocumentSurfaceRenderTests.Created("");
        var cut = Render(document);
        var id = document.Blocks[0].Id;

        await Paste(cut, Sync(id, 0, id, 0), text: "one\r\ntwo");

        Assert.Equal(["one", "two"], Texts(document));
        document.Undo.Undo();
        Assert.Equal([""], Texts(document));
    }

    [Fact]
    public async Task Paste_PngImage_InsertsImage()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        var id = document.Blocks[0].Id;

        await Paste(cut, Sync(id, 5, id, 5), image: Document.ObjectCommandTests.Png(4, 4));

        Assert.Single(document.AllParagraphs().SelectMany(p => p.Inlines).OfType<InlineImage>());
    }

    [Fact]
    public async Task Paste_HtmlWithOnlyRemoteImage_FallsBackToImageFile()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document);
        var id = document.Blocks[0].Id;

        await Paste(cut, Sync(id, 5, id, 5), html: "<img src=\"http://evil/x.png\">", image: Document.ObjectCommandTests.Png(4, 4));

        Assert.Single(document.AllParagraphs().SelectMany(p => p.Inlines).OfType<InlineImage>());
        Assert.Equal(["Hello\uFFFC"], Texts(document));
    }

    [Fact]
    public async Task Paste_ReadOnly_Ignored()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello");
        var cut = Render(document, readOnly: true);
        var id = document.Blocks[0].Id;
        var version = document.Version;

        await Paste(cut, Sync(id, 0, id, 0), text: "x");

        Assert.Equal(version, document.Version);
    }

    [Fact]
    public async Task Cut_StoresClipAndDeletesSelection_OneStep()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world", "Second");
        var cut = Render(document);
        var ids = document.Blocks.Select(b => b.Id).ToList();
        document.Undo.Clear();

        await Copy(cut, Sync(ids[0], 5, ids[0], 11), Clip, true);

        Assert.Equal(["Hello", "Second"], Texts(document));
        await Paste(cut, Sync(ids[1], 6, ids[1], 6), clip: Clip);
        Assert.Equal(["Hello", "Second world"], Texts(document));
        document.Undo.Undo();
        document.Undo.Undo();
        Assert.Equal(["Hello world", "Second"], Texts(document));
        Assert.False(document.Undo.CanUndo);
    }

    [Fact]
    public async Task Copy_ReadOnly_StoresClip()
    {
        var document = DocumentSurfaceRenderTests.Created("Hello world");
        var cut = Render(document, readOnly: true);
        var id = document.Blocks[0].Id;
        var version = document.Version;

        await Copy(cut, Sync(id, 0, id, 5), Clip);
        await Copy(cut, Sync(id, 5, id, 11), OtherClip, true);

        Assert.Equal(version, document.Version);
        Assert.Equal(["Hello world"], Texts(document));
        await cut.InvokeAsync(() => cut.Render(p => p.Add(x => x.ReadOnly, false)));
        await Paste(cut, Sync(id, 11, id, 11), clip: Clip);
        Assert.Equal(["Hello worldHello"], Texts(document));
    }

    [Fact]
    public async Task RibbonButtons_CallTheSurface()
    {
        var cut = Render(DocumentSurfaceRenderTests.Created("Hello"));

        await Run(cut, "wd-copy");
        await Run(cut, "wd-cut");
        await Run(cut, "wd-paste");

        _context.JSInterop.VerifyInvoke("copy", 2);
        _context.JSInterop.VerifyInvoke("paste", 1);
    }
}
