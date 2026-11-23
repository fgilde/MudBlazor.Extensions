using System.Globalization;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Document;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentSurfaceRenderTests
{
    private const string Injection = "x';}body{background:url(evil)}";
    internal static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

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
        var mark = new RunFormat { Bold = true, Underline = "single", SizePt = 16 };
        var document = TextDocument.FromBlocks([new Paragraph { MarkFormat = mark, Inlines = [new TextRun("x") { Format = new RunFormat { SizePt = 8 } }] }, new Paragraph { MarkFormat = mark }]);
        var render = new DocumentSurfaceRender(document);

        var filled = render.Render(document.Blocks[0]);
        var empty = render.Render(document.Blocks[1]);

        Assert.DoesNotContain("font-weight", filled.Css);
        Assert.Contains("font-size:8pt", filled.Css);
        Assert.DoesNotContain("font-weight", filled.Runs[0].Css);
        Assert.DoesNotContain("font-size", filled.Runs[0].Css);
        Assert.Contains("font-weight:bold", empty.Css);
        Assert.Contains("font-size:16pt", empty.Css);
        Assert.DoesNotContain("text-decoration", empty.Css);
    }

    [Fact]
    public void Css_InheritedFormats_AreResetExplicitly()
    {
        var basis = new RunFormat { Caps = true, SmallCaps = true, Color = "#FF0000", Underline = "single", Strike = true, Highlight = "yellow", VerticalAlign = VerticalAlign.Superscript };
        var off = new RunFormat { Caps = false, SmallCaps = false, Color = "auto", Underline = "none", Strike = false, VerticalAlign = VerticalAlign.Baseline };

        var css = DocumentCss.Run(off, basis);
        var same = DocumentCss.Run(basis, basis);

        foreach (var reset in new[] { "text-transform:none;", "font-variant:normal;", "color:#000000;", "text-decoration:none;", "background:none;", "vertical-align:baseline;" })
        {
            Assert.Contains(reset, css);
            Assert.DoesNotContain(reset, same);
        }

        Assert.DoesNotContain("color:#000000", DocumentCss.Run(off, off with { Color = "auto" }));
    }

    [Fact]
    public void Css_HostileNumbers_AreClamped()
    {
        var document = TextDocument.FromBlocks(
        [
            new Paragraph
            {
                Format = new ParagraphFormat { IndentLeft = int.MaxValue, IndentFirstLine = int.MinValue, IndentRight = -int.MaxValue, SpacingBefore = -500, SpacingAfter = int.MaxValue, Line = new LineSpacing(int.MaxValue, LineRule.Exact) },
                Inlines = [new TextRun("x") { Format = new RunFormat { SizePt = 1e9 } }, new TextRun("y") { Format = new RunFormat { SizePt = 0.25 } }, new InlineImage("p", long.MaxValue, 0)]
            },
            new Paragraph { Format = new ParagraphFormat { Line = new LineSpacing(int.MaxValue, LineRule.Auto) } },
            new Paragraph { Format = new ParagraphFormat { Line = new LineSpacing(1, LineRule.Auto) } },
            new Table { Grid = [int.MaxValue, -5], Rows = [new TableRow([new TableCell([new Paragraph()]) { GridSpan = 2 }]) { Height = int.MaxValue }] }
        ]);
        document.PutMedia("p", [1], "image/png");
        var render = new DocumentSurfaceRender(document);

        var paragraph = render.Render(document.Blocks[0]);
        var table = render.Render(document.Blocks[3]);

        Assert.Contains("margin-left:1584pt", paragraph.Css);
        Assert.Contains("text-indent:-1584pt", paragraph.Css);
        Assert.Contains("margin-right:-1584pt", paragraph.Css);
        Assert.Contains("margin-top:0pt", paragraph.Css);
        Assert.Contains("margin-bottom:1584pt", paragraph.Css);
        Assert.Contains("line-height:1584pt", paragraph.Css);
        Assert.Contains("font-size:1638pt", paragraph.Runs[0].Css);
        Assert.Contains("font-size:1pt", paragraph.Runs[1].Css);
        Assert.Equal((10000, 1), (paragraph.Runs[2].W, paragraph.Runs[2].H));
        Assert.Contains("line-height:132;", render.Render(document.Blocks[1]).Css);
        Assert.Contains("line-height:0.06;", render.Render(document.Blocks[2]).Css);
        Assert.Equal([2112, 0], table.Grid);
        Assert.Equal("height:1584pt;", table.Rows[0].Css);
    }

    [Fact]
    public void Paragraph_FontSize_IsTheSmallestRunSize()
    {
        static TextRun Run(string text, double? size) => new(text) { Format = new RunFormat { SizePt = size } };
        var document = TextDocument.FromBlocks(
        [
            new Paragraph { Inlines = [Run("a", 8), Run("b", 8)] },
            new Paragraph { Inlines = [Run("a", 12), Run("b", 8), Run("c", 0)] }
        ]);
        document.Styles = new DocumentStyles([], new RunFormat { SizePt = 11 }, null);
        var render = new DocumentSurfaceRender(document);

        var small = render.Render(document.Blocks[0]);
        var mixed = render.Render(document.Blocks[1]);

        Assert.Contains("font-size:8pt", small.Css);
        Assert.DoesNotContain("font-size", small.Runs[0].Css);
        Assert.Contains("font-size:8pt", mixed.Css);
        Assert.Equal(["font-size:12pt;", "", "font-size:11pt;"], mixed.Runs.Select(r => r.Css));
        Assert.Equal("", render.RunCss((Paragraph)document.Blocks[1], new RunFormat { SizePt = 8 }));
        Assert.Equal("font-size:12pt;", render.RunCss((Paragraph)document.Blocks[1], new RunFormat { SizePt = 12 }));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-3d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Css_InvalidFontSize_IsUnset(double size)
    {
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("x") { Format = new RunFormat { SizePt = size } }] }, new Paragraph()]);
        document.Styles = new DocumentStyles([], new RunFormat { SizePt = size }, null);
        var render = new DocumentSurfaceRender(document);

        Assert.Equal("", DocumentCss.Run(new RunFormat { SizePt = size }, new RunFormat { SizePt = 11 }));
        Assert.DoesNotContain("font-size", render.SheetCss());
        Assert.DoesNotContain("font-size", render.Render(document.Blocks[0]).Css);
        Assert.DoesNotContain("font-size", render.Render(document.Blocks[0]).Runs[0].Css);
        Assert.Contains("body{}", Html(document));
    }

    [Fact]
    public void Html_ImageSizeAndDefaultFontSize_AreClamped()
    {
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new InlineImage("p", long.MaxValue, 0)] }]);
        document.PutMedia("p", [1], "image/png");
        document.Styles = new DocumentStyles([], new RunFormat { SizePt = 1e9 }, null);

        var html = Html(document);

        Assert.Contains("width=\"10000\" height=\"1\"", html);
        Assert.Contains("body{font-size:1638pt;}", html);
    }

    [Fact]
    public void Table_ZeroSpanCell_TakesOneGridColumn()
    {
        var document = TextDocument.FromBlocks(
        [
            new Table
            {
                Grid = [1500, 1500],
                Rows =
                [
                    new TableRow([new TableCell([new Paragraph { Inlines = [new TextRun("a")] }]) { GridSpan = 0 }, new TableCell([new Paragraph { Inlines = [new TextRun("b")] }]) { VMerge = VerticalMerge.Restart }]),
                    new TableRow([new TableCell([new Paragraph { Inlines = [new TextRun("c")] }]), new TableCell([new Paragraph()]) { VMerge = VerticalMerge.Continue }])
                ]
            }
        ]);
        var table = (Table)document.Blocks[0];

        var block = new DocumentSurfaceRender(document).Render(table);

        Assert.Equal([0, 1], Tables.Layout(table.Rows[0]).Select(p => p.Column));
        Assert.Equal(2, block.Rows[0].Cells[1].RowSpan);
        Assert.Equal(["c"], block.Rows[1].Cells.Select(c => c.Blocks[0].Text));
    }

    [Fact]
    public void Image_WithoutMedia_IsAPlaceholderToken()
    {
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new InlineImage("missing", 9525, 9525) { AltText = "logo" }, new InlineImage("gone", 9525, 9525)] }]);
        var render = new DocumentSurfaceRender(document);

        var block = render.Render(document.Blocks[0]);

        Assert.Equal([("o", "logo", null), ("o", "", null)], block.Runs.Select(r => (r.K, r.T, r.Media)));
        Assert.Empty(render.TakeMedia());
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
        var document = TextDocument.FromBlocks([new Paragraph { Inlines = [new TextRun("x") { Format = new RunFormat { Font = "Arial" + Injection } }] }, new Paragraph { MarkFormat = new RunFormat { Font = Injection } }]);
        var render = new DocumentSurfaceRender(document);

        var block = render.Render(document.Blocks[0]);
        var empty = render.Render(document.Blocks[1]);

        foreach (var css in new[] { empty.Css, block.Runs[0].Css })
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

            Assert.Contains("font-size:10.5pt", block.Css);
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
    public void Table_DirectNilBorders_WinOverTheStyle()
    {
        var document = WithTable();
        var table = document.Blocks.OfType<Table>().Single();
        var render = new DocumentSurfaceRender(document);
        Assert.Contains("border:0.5pt solid", render.Render(table).Rows[0].Cells[0].Css);
        var nil = new Border("nil", 0, "auto");

        document.Execute(new SetTableFormatCommand(table.Id, f => f with { Borders = new TableBorders(nil, nil, nil, nil, nil, nil) }, "Borders"));
        var block = render.Render(document.Blocks.OfType<Table>().Single());

        Assert.All(block.Rows.SelectMany(r => r.Cells), c => Assert.Contains("dotted", c.Css));
    }

    [Fact]
    public void Table_OrphanContinuationAndZeroSpan_RenderAsNormalCells()
    {
        static TableCell Cell(string text, int span = 1, VerticalMerge merge = VerticalMerge.None) =>
            new([new Paragraph { Inlines = [new TextRun(text)] }]) { GridSpan = span, VMerge = merge };
        var document = TextDocument.FromBlocks(
        [
            new Table
            {
                Grid = [1500, 1500],
                Rows =
                [
                    new TableRow([Cell("a", 1, VerticalMerge.Continue), Cell("b", 0)]),
                    new TableRow([Cell("c"), Cell("d", 1, VerticalMerge.Continue)])
                ]
            }
        ]);

        var block = new DocumentSurfaceRender(document).Render(document.Blocks[0]);

        Assert.Equal([("a", 1, 1), ("b", 1, 1)], block.Rows[0].Cells.Select(c => (c.Blocks[0].Text, c.ColSpan, c.RowSpan)));
        Assert.Equal(["c", "d"], block.Rows[1].Cells.Select(c => c.Blocks[0].Text));
        Assert.Equal(4, Html(document).Split("<td").Length - 1);
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

    internal static List<string> AppliedOrder(IEnumerable<string> before, SurfacePatch patch)
    {
        var order = before.Except(patch.Remove).ToList();
        foreach (var upsert in patch.Upsert.Where(u => !u.InPlace))
        {
            order.Remove(upsert.Block.Id);
            Assert.True(upsert.After == null || order.Contains(upsert.After), $"{upsert.After} missing for {upsert.Block.Id}");
            order.Insert(upsert.After == null ? 0 : order.IndexOf(upsert.After) + 1, upsert.Block.Id);
        }

        return order;
    }

    internal static string Html(TextDocument document)
    {
        using var stream = new MemoryStream();
        document.Save(stream, DocumentFileFormat.Html);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static TextDocument Created(params string[] texts)
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 0)), string.Concat(texts)));
        foreach (var text in texts.SkipLast(1)) document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[^1].Id, text.Length)));
        document.Styles = StyleDefinitions.With(document.Styles, Tables.GridStyle, StyleType.Table);
        return document;
    }

    internal static TextDocument Paragraphs(params string[] texts) =>
        TextDocument.FromBlocks(texts.Select(t => new Paragraph { Inlines = [new TextRun(t)] }));

    internal static TextDocument WithTable()
    {
        var document = TextDocument.Create();
        var id = document.Blocks[0].Id;
        document.Execute(new ReplaceTextCommand(new DocumentRange(document.PositionOf(id, 0), document.PositionOf(id, 0)), "before"));
        document.Execute(new InsertTableCommand(document.PositionOf(id, 6), 2, 2));
        return document;
    }

    internal static string Cell(TextDocument document, int row, int cell) =>
        document.Blocks.OfType<Table>().Single().Rows[row].Cells[cell].Blocks[0].Id;

    internal static List<TextDocumentChangedEventArgs> Record(TextDocument document)
    {
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);
        return events;
    }
}
