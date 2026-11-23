using System.Globalization;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Document;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentSurfaceRenderTests
{
    private const string Injection = "x';}body{background:url(evil)}";
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Paragraph_RunsCarryKindsAndCss()
    {
        const string part = "word/media/image1.png";
        var paragraph = new Paragraph
        {
            Inlines =
            [
                new TextRun("a"), new Tab(), new TextRun("b"), new TextRun("c") { Format = new RunFormat { Bold = true } }, new Break(BreakKind.Line),
                new OpaqueInline("field", "7"), new InlineImage(part, 9525 * 10, 9525 * 5)
            ]
        };
        var document = TextDocument.FromBlocks([paragraph]);
        document.PutMedia(part, [1, 2, 3], "image/png");
        var render = new DocumentSurfaceRender(document);

        var block = render.Render(document.Blocks[0]);

        Assert.Equal("p", block.K);
        Assert.Equal(["t", "t", "v", "o", "i"], block.Runs.Select(r => r.K));
        Assert.Equal("a\tb", block.Runs[0].T);
        Assert.Contains("font-weight:bold", block.Runs[1].Css);
        Assert.Equal("7", block.Runs[3].T);
        Assert.Equal((part, 10, 5), (block.Runs[4].Media, block.Runs[4].W, block.Runs[4].H));
        Assert.Equal(((Paragraph)document.Blocks[0]).Text, block.Text);
        Assert.Equal([part], render.TakeMedia());
        Assert.Empty(render.TakeMedia());
    }

    [Fact]
    public void Paragraph_HiddenRun_DisplayNone()
    {
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("x") { Format = new RunFormat { Hidden = true } }, new TextRun("y")] }]);

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        Assert.Contains("display:none", block.Runs[0].Css);
        Assert.DoesNotContain("display:none", block.Runs[1].Css);
    }

    [Fact]
    public void Paragraph_NonBoldRunInBoldMark_ResetsWeight()
    {
        var document = TextDocument.FromBlocks([new Paragraph { MarkFormat = new RunFormat { Bold = true, Underline = "single" }, Inlines = [new TextRun("x")] }]);

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        Assert.Contains("font-weight:bold", block.Css);
        Assert.DoesNotContain("text-decoration", block.Css);
        Assert.Contains("font-weight:normal", block.Runs[0].Css);
    }

    [Fact]
    public void ListParagraph_LabelAndHangingIndent()
    {
        var document = TextDocument.Create();
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(document.Blocks[0].Id, 0), document.PositionOf(document.Blocks[0].Id, 0)), "item"));
        document.Execute(new ApplyListCommand([document.Blocks[0].Id], ListTemplate.Decimal));

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        Assert.Equal("1.", block.Label);
        Assert.Contains("text-indent:-18pt", block.Css);
        Assert.Contains("margin-left:36pt", block.Css);
        Assert.Contains("min-width:18pt", block.LabelCss);
    }

    [Fact]
    public void Css_FontNameInjection_IsSanitised()
    {
        var document = TextDocument.FromBlocks([new Paragraph { MarkFormat = new RunFormat { Font = Injection }, Inlines = [new TextRun("x") { Format = new RunFormat { Font = "Arial" + Injection } }] }]);

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        foreach (var css in new[] { block.Css, block.Runs[0].Css })
        {
            Assert.Contains("font-family:", css);
            Assert.DoesNotContain(";}", css);
            Assert.DoesNotContain("url(", css);
            Assert.Equal(2, css.Count(c => c == '\''));
        }
    }

    [Fact]
    public void Css_ColourShadingAndHighlightInjection_AreDropped()
    {
        var table = new Table
        {
            Grid = [1000],
            Rows = [new TableRow([new TableCell([new Paragraph()]) { Shading = "#FF0000;}x{" }])]
        };
        var document = TextDocument.FromBlocks(
        [
            new Paragraph
            {
                Format = new ParagraphFormat { Shading = "red;background:url(x)" },
                Inlines = [new TextRun("x") { Format = new RunFormat { Color = "#FF0000;}x{", Shading = "url(x)", Highlight = "yellow;}" } }, new TextRun("y") { Format = new RunFormat { Color = "#00ff00" } }]
            },
            table
        ]);
        var render = new DocumentSurfaceRender(document);

        var paragraph = render.Render(document.Blocks[0]);
        var cell = render.Render(document.Blocks[1]).Rows[0].Cells[0];

        foreach (var css in new[] { paragraph.Css, paragraph.Runs[0].Css, cell.Css })
        {
            Assert.DoesNotContain(";}", css);
            Assert.DoesNotContain("url(", css);
            Assert.DoesNotContain("red", css);
            Assert.DoesNotContain("background-color", css);
        }

        Assert.Contains("color:#00FF00", paragraph.Runs[1].Css);
    }

    [Fact]
    public void Css_ListLabelText_IsTextNotCss()
    {
        var document = new DocxBuilder()
            .Numbering("<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1;}&lt;b&gt;\"/>" +
                       "<w:rPr><w:rFonts w:ascii=\"a';}b\"/><w:color w:val=\"FF0000;}\"/></w:rPr></w:lvl></w:abstractNum><w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\"/></w:num>")
            .Body("<w:p><w:pPr><w:numPr><w:ilvl w:val=\"0\"/><w:numId w:val=\"1\"/></w:numPr></w:pPr><w:r><w:t>a</w:t></w:r></w:p>")
            .Load();

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        Assert.Equal("1;}<b>", block.Label);
        Assert.DoesNotContain(";}", block.LabelCss);
        Assert.DoesNotContain("<", block.LabelCss);
    }

    [Fact]
    public void Css_SymbolFontLabel_UsesTheMarkFont()
    {
        var document = TextDocument.Create();
        document.Execute(new ApplyListCommand([document.Blocks[0].Id], ListTemplate.Bullet));

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        Assert.DoesNotContain("Symbol", block.LabelCss);
        Assert.NotNull(block.Label);
    }

    [Fact]
    public void Css_Numbers_AreInvariant()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var document = TextDocument.FromBlocks([new Paragraph { Format = new ParagraphFormat { SpacingBefore = 30, Line = new LineSpacing(480, LineRule.Auto) }, Inlines = [new TextRun("x") { Format = new RunFormat { SizePt = 10.5 } }] }]);

            var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

            Assert.Contains("font-size:10.5pt", block.Runs[0].Css);
            Assert.Contains("margin-top:1.5pt", block.Css);
            Assert.Contains("line-height:2.3;", block.Css);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Css_FontStack_CalibriFallsBackToCarlito()
    {
        Assert.Equal("'Calibri','Carlito',sans-serif", DocumentCss.FontStack("Calibri"));
        Assert.Equal("'Times New Roman','Liberation Serif',serif", DocumentCss.FontStack("Times New Roman"));
        Assert.Equal("'Foo',sans-serif", DocumentCss.FontStack("Foo"));
        Assert.Contains("font-family:'Calibri','Carlito',sans-serif", new DocumentSurfaceRender(TextDocument.Create()).SheetCss());
    }

    [Fact]
    public void SheetCss_PageWidthAndMargins()
    {
        var css = new DocumentSurfaceRender(TextDocument.Create(PageSetup.Letter)).SheetCss();

        Assert.Contains("width:612pt", css);
        Assert.Contains("padding:72pt 72pt 72pt 72pt", css);
        Assert.Contains("font-size:11pt", css);
    }

    [Fact]
    public void Table_MergedCells_ColSpanAndRowSpan()
    {
        static TableCell Cell(string text, int span = 1, VerticalMerge merge = VerticalMerge.None) =>
            new([new Paragraph { Inlines = [new TextRun(text)] }]) { GridSpan = span, VMerge = merge };
        var document = TextDocument.FromBlocks(
        [
            new Table
            {
                Grid = [1500, 1500, 3000],
                Format = new TableFormat { Borders = new TableBorders(new Border("single", 0.5, "auto"), null, null, null, null, null) },
                Rows =
                [
                    new TableRow([Cell("a", 2), Cell("m", 1, VerticalMerge.Restart)]),
                    new TableRow([Cell("b"), Cell("c"), Cell("", 1, VerticalMerge.Continue)]),
                    new TableRow([Cell("d"), Cell("e"), Cell("", 1, VerticalMerge.Continue)])
                ]
            }
        ]);

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        Assert.Equal("t", block.K);
        Assert.Equal([100, 100, 200], block.Grid);
        Assert.Equal((2, 1), (block.Rows[0].Cells[0].ColSpan, block.Rows[0].Cells[0].RowSpan));
        Assert.Equal((1, 3), (block.Rows[0].Cells[1].ColSpan, block.Rows[0].Cells[1].RowSpan));
        Assert.Equal(2, block.Rows[1].Cells.Count);
        Assert.Equal(2, block.Rows[2].Cells.Count);
        Assert.Equal("m", block.Rows[0].Cells[1].Blocks[0].Text);
        Assert.All(block.Rows.SelectMany(r => r.Cells), c => Assert.Contains("border:0.5pt solid", c.Css));
    }

    [Fact]
    public void OpaqueBlocks_VisibleWithPreview_HiddenAsH()
    {
        var document = TextDocument.FromBlocks(
        [
            new OpaqueBlock { Kind = "contentControl", Preview = [new Paragraph { Inlines = [new TextRun("p")] }] },
            new OpaqueBlock { Kind = "marker", Visible = false }
        ]);
        var render = new DocumentSurfaceRender(document);

        var visible = render.Render(document.Blocks[0]);
        var hidden = render.Render(document.Blocks[1]);

        Assert.Equal("o", visible.K);
        Assert.Equal("p", Assert.Single(visible.Blocks).Text);
        Assert.Equal("h", hidden.K);
        Assert.Equal(document.Blocks[1].Id, hidden.Id);
    }

    [Fact]
    public void Patch_Typing_InPlaceUpsertOfThatParagraphOnly()
    {
        var document = Paragraphs("a", "b", "c");
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var id = document.Blocks[1].Id;

        document.ApplyTyping([new TypedParagraph(id, "bx", 2)], T0);
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 5);

        Assert.Equal(5, patch.Seq);
        Assert.False(patch.Reset);
        Assert.Null(patch.SheetCss);
        Assert.Empty(patch.Remove);
        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal((id, "bx"), (upsert.Block.Id, upsert.Block.Text));
    }

    [Fact]
    public void Patch_Split_InsertsAfterThePreviousBlock()
    {
        var document = Paragraphs("a", "bc", "d");
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var (first, second) = (document.Blocks[0].Id, document.Blocks[1].Id);

        document.Execute(new SplitParagraphCommand(document.PositionOf(second, 1)));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        Assert.Equal(2, patch.Upsert.Count);
        Assert.All(patch.Upsert, u => Assert.False(u.InPlace));
        Assert.Equal((second, first), (patch.Upsert[0].Block.Id, patch.Upsert[0].After));
        Assert.Equal((document.Blocks[2].Id, second), (patch.Upsert[1].Block.Id, patch.Upsert[1].After));
        Assert.Equal("c", patch.Upsert[1].Block.Text);
    }

    [Fact]
    public void Patch_TypingInCell_UpsertsTheNestedParagraphOnly()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var cellParagraph = Cell(document, 0, 1);

        document.ApplyTyping([new TypedParagraph(cellParagraph, "x", 1)], T0);
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal(("p", cellParagraph, "x"), (upsert.Block.K, upsert.Block.Id, upsert.Block.Text));
    }

    [Fact]
    public void Patch_CellShadingChanged_UpsertsTheWholeTable()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var table = document.Blocks.OfType<Table>().Single();

        document.Execute(new SetTableCellFormatCommand(table.Id, [(0, 0)], c => c with { Shading = "#FFFF00" }, "Shading"));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var upsert = Assert.Single(patch.Upsert);
        Assert.Equal(("t", table.Id), (upsert.Block.K, upsert.Block.Id));
        Assert.Contains("background-color:#FFFF00", upsert.Block.Rows[0].Cells[0].Css);
    }

    [Fact]
    public void Patch_RowInserted_UpsertsTheWholeTable()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);
        var table = document.Blocks.OfType<Table>().Single();
        var index = document.BlockIndex(table.Id);

        document.Execute(new InsertTableRowsCommand(table.Id, 1, 1));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        var upsert = Assert.Single(patch.Upsert);
        Assert.Equal(("t", table.Id, document.Blocks[index - 1].Id), (upsert.Block.K, upsert.Block.Id, upsert.After));
        Assert.Equal(3, upsert.Block.Rows.Count);
        var ids = DocumentSurfacePatches.Ids(patch).ToList();
        Assert.Contains(table.Id, ids);
        Assert.Contains(Cell(document, 1, 0), ids);
        Assert.Equal(1 + 6, ids.Count);
    }

    [Fact]
    public void Patch_ListItemInserted_RefreshesFollowingLabels()
    {
        var document = Paragraphs("1", "2", "3", "4", "5");
        document.Execute(new ApplyListCommand(document.Blocks.Select(b => b.Id).ToList(), ListTemplate.Decimal));
        var render = new DocumentSurfaceRender(document);
        var seq = 0;
        DocumentSurfacePatches.Initial(document, render, () => ++seq);
        var events = Record(document);

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[1].Id, 1)));
        var patch = DocumentSurfacePatches.Change(document, render, events[0], ++seq);

        var inserted = Assert.Single(patch.Upsert, u => !u.InPlace);
        Assert.Equal((document.Blocks[2].Id, document.Blocks[1].Id, "3."), (inserted.Block.Id, inserted.After, inserted.Block.Label));
        Assert.Equal(["4.", "5.", "6."], patch.Upsert.Where(u => u.InPlace).Select(u => u.Block.Label));

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[0].Id, 1)));
        var next = DocumentSurfacePatches.Change(document, render, events[1], ++seq);

        Assert.Equal(["3.", "4.", "5.", "6.", "7."], next.Upsert.Where(u => u.InPlace).Select(u => u.Block.Label));
    }

    [Fact]
    public void Patch_StyleChanged_IsReset()
    {
        var document = Paragraphs("a", "b");
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);

        document.Execute(new SetDefaultFontCommand("Arial", 12));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 3);

        Assert.True(patch.Reset);
        Assert.Contains("'Arial','Liberation Sans',sans-serif", patch.SheetCss);
        Assert.Equal(document.Blocks.Select(b => b.Id), patch.Upsert.Select(u => u.Block.Id));
        Assert.Null(patch.Upsert[0].After);
        Assert.Equal(document.Blocks[0].Id, patch.Upsert[1].After);
    }

    [Fact]
    public void Patch_PageSetup_SendsSheetCssOnly()
    {
        var document = TextDocument.Create();
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);

        document.Execute(new SetPageSetupCommand(document.Sections[0].Id, PageSetup.Letter));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        Assert.Contains("width:612pt", patch.SheetCss);
        Assert.False(patch.Reset);
        Assert.Empty(patch.Upsert);
    }

    [Fact]
    public void Patch_HeaderChanged_ProducesNothing()
    {
        var document = TextDocument.Create();
        document.Execute(new AddHeaderFooterCommand(document.Sections[0].Id, HeaderFooterKind.Header, HeaderFooterType.Default));
        var header = document.HeadersFooters[0].Blocks[0].Id;
        var render = new DocumentSurfaceRender(document);
        var events = Record(document);

        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(header, 0), document.PositionOf(header, 0)), "head"));
        var patch = DocumentSurfacePatches.Change(document, render, Assert.Single(events), 1);

        Assert.Empty(patch.Upsert);
        Assert.Empty(patch.Remove);
        Assert.Null(patch.SheetCss);
    }

    [Fact]
    public void Refresh_UpsertsKnownParagraphsInPlace()
    {
        var document = WithTable();
        var render = new DocumentSurfaceRender(document);
        var cellParagraph = Cell(document, 1, 1);

        var patch = DocumentSurfacePatches.Refresh(document, render, [cellParagraph, "unknown", document.Blocks.OfType<Table>().Single().Id], 4);

        Assert.Equal(4, patch.Seq);
        var upsert = Assert.Single(patch.Upsert);
        Assert.True(upsert.InPlace);
        Assert.Equal(cellParagraph, upsert.Block.Id);
    }

    [Fact]
    public void Initial_ChunksOf500_FirstIsReset()
    {
        var document = TextDocument.FromBlocks(Enumerable.Range(0, 1200).Select(i => new Paragraph { Inlines = [new TextRun(i.ToString(CultureInfo.InvariantCulture))] }));
        var seq = 0;

        var patches = DocumentSurfacePatches.Initial(document, new DocumentSurfaceRender(document), () => ++seq);

        Assert.Equal([1, 2, 3], patches.Select(p => p.Seq));
        Assert.Equal([true, false, false], patches.Select(p => p.Reset));
        Assert.NotNull(patches[0].SheetCss);
        Assert.Null(patches[1].SheetCss);
        Assert.Equal([500, 500, 200], patches.Select(p => p.Upsert.Count));
        Assert.Null(patches[0].Upsert[0].After);
        Assert.Equal(patches[0].Upsert[^1].Block.Id, patches[1].Upsert[0].After);
        Assert.Equal(document.Blocks.Select(b => b.Id), patches.SelectMany(p => p.Upsert).Select(u => u.Block.Id));
    }

    private static TextDocument Paragraphs(params string[] texts) =>
        TextDocument.FromBlocks(texts.Select(t => new Paragraph { Inlines = [new TextRun(t)] }));

    private static TextDocument WithTable()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 0)), "before"));
        document.Execute(new InsertTableCommand(document.PositionOf(id, 6), 2, 2));
        return document;
    }

    private static string Cell(TextDocument document, int row, int cell) =>
        document.Blocks.OfType<Table>().Single().Rows[row].Cells[cell].Blocks[0].Id;

    private static List<TextDocumentChangedEventArgs> Record(TextDocument document)
    {
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);
        return events;
    }
}
