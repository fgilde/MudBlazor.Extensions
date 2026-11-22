using System.Text;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class RtfFormatTests
{
    [Fact]
    public void Rtf_Read_BoldItalicColorAlignmentTable()
    {
        var document = Load(
            @"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fswiss Arial;}{\f1\fmodern Courier New;}}{\colortbl;\red255\green0\blue0;}" +
            @"\pard\qc{\b bold} {\i italic} {\cf1 red} {\f1\fs32 mono}\par" +
            @"\trowd\trgaph108\cellx2000\cellx4000\pard\intbl A\cell {\b B}\cell\row" +
            @"\pard\qr after\par}");

        var first = Assert.IsType<Paragraph>(document.Blocks[0]);
        Assert.Equal("bold italic red mono", first.Text);
        Assert.Equal(ParagraphAlignment.Center, first.Format.Alignment);
        Assert.True(Run(first, "bold").Format.Bold);
        Assert.True(Run(first, "italic").Format.Italic);
        Assert.Null(Run(first, "italic").Format.Bold);
        Assert.Equal("#FF0000", Run(first, "red").Format.Color);
        Assert.Equal(("Courier New", 16.0), (Run(first, "mono").Format.Font, Run(first, "mono").Format.SizePt));
        Assert.Equal("Arial", Run(first, "bold").Format.Font);

        var table = Assert.IsType<Table>(document.Blocks[1]);
        var cells = Assert.Single(table.Rows).Cells;
        Assert.Equal(["A", "B"], cells.Select(c => ((Paragraph)Assert.Single(c.Blocks)).Text));
        Assert.True(((Paragraph)cells[1].Blocks[0]).Inlines[0].Format.Bold);

        var last = Assert.IsType<Paragraph>(document.Blocks[2]);
        Assert.Equal(("after", ParagraphAlignment.Right), (last.Text, last.Format.Alignment));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void Rtf_Read_UnicodeAndCodepageEscapes()
    {
        Assert.Equal("\u00FC\u00FC \U0001F600 \u20AC", Single(Load(@"{\rtf1\ansi\ansicpg1252\deff0 \u252?\'fc \u-10179?\u-8704? \'80}")).Text);
        Assert.Equal("\u0410", Single(Load(@"{\rtf1\ansi\ansicpg1251\deff0 \'c0}")).Text);
    }

    [Fact]
    public void Rtf_Read_TabsLineBreaksSpacesIndentsAndEmptyCells()
    {
        var document = Load(
            @"{\rtf1\ansi\deff0\pard\li720\fi-360 \tab a\tab\tab b  c\line d\par" +
            @"\trowd\cellx1000\cellx2000\pard\intbl\cell x\cell\row}");

        var paragraph = Assert.IsType<Paragraph>(document.Blocks[0]);
        Assert.Equal("\ta\t\tb  c\vd", paragraph.Text);
        Assert.Equal(3, paragraph.Inlines.Count(i => i is Tab));
        Assert.Equal(BreakKind.Line, Assert.Single(paragraph.Inlines.OfType<Break>()).Kind);
        Assert.Equal((720, -360), (paragraph.Format.IndentLeft, paragraph.Format.IndentFirstLine));
        var table = Assert.IsType<Table>(document.Blocks[1]);
        Assert.Equal(["", "x"], table.Rows[0].Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
    }

    [Fact]
    public void Rtf_Write_EscapesBracesBackslashUnicode()
    {
        var text = "{a}\\\u00FC \U0001F600\uFB01";
        var bytes = Save(TextFormat.FromText(text));

        Assert.All(bytes, b => Assert.True(b is >= 0x20 and < 0x7F or (byte)'\r' or (byte)'\n', $"byte {b}"));
        var rtf = Encoding.ASCII.GetString(bytes);
        Assert.StartsWith(@"{\rtf1\ansi\ansicpg1252\deff0", rtf);
        Assert.Contains(@"\{a\}\\\u252?", rtf);
        Assert.Contains(@"\u-10179?\u-8704?\u-1279?", rtf);
        Assert.Equal(text, Single(Load(bytes)).Text);
    }

    [Fact]
    public void Rtf_WriteThenRead_KeepsFormatsTableImageLink()
    {
        var png = ObjectCommandTests.Png(40, 20);
        var source = HtmlReader.Read(
            "<h1>Title</h1>" +
            "<p style=\"text-align:center\"><b>bold</b> <i>italic</i> <u>under</u> <s>strike</s> <span style=\"color:#FF0000\">red</span> " +
            "<span style=\"font-family:'Courier New';font-size:16pt\">mono</span> <sup>up</sup> <span style=\"background-color:yellow\">mark</span></p>" +
            "<table><tr><td colspan=\"2\">A</td></tr><tr><td>B</td><td><b>C</b></td></tr></table>" +
            $"<p><a href=\"https://example.com/?a=1&amp;b={{x}}\">link</a> <img src=\"data:image/png;base64,{Convert.ToBase64String(png)}\" width=\"40\" height=\"20\"></p>");

        var bytes = Save(source);
        var document = Load(bytes);

        var heading = Assert.IsType<Paragraph>(document.Blocks[0]);
        Assert.Equal(("Title", "Heading1"), (heading.Text, heading.Format.StyleId));
        var formatted = Assert.IsType<Paragraph>(document.Blocks[1]);
        Assert.Equal("bold italic under strike red mono up mark", formatted.Text);
        Assert.Equal(ParagraphAlignment.Center, formatted.Format.Alignment);
        Assert.True(Run(formatted, "bold").Format.Bold);
        Assert.True(Run(formatted, "italic").Format.Italic);
        Assert.Equal("single", Run(formatted, "under").Format.Underline);
        Assert.True(Run(formatted, "strike").Format.Strike);
        Assert.Equal("#FF0000", Run(formatted, "red").Format.Color);
        Assert.Equal(("Courier New", 16.0), (Run(formatted, "mono").Format.Font, Run(formatted, "mono").Format.SizePt));
        Assert.Equal(VerticalAlign.Superscript, Run(formatted, "up").Format.VerticalAlign);
        Assert.Equal("yellow", Run(formatted, "mark").Format.Highlight);

        var table = Assert.IsType<Table>(document.Blocks[2]);
        Assert.Equal(2, table.Rows.Count);
        var merged = Assert.Single(table.Rows[0].Cells);
        Assert.Equal((2, "A"), (merged.GridSpan, ((Paragraph)merged.Blocks[0]).Text));
        Assert.Equal(["B", "C"], table.Rows[1].Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
        Assert.True(((Paragraph)table.Rows[1].Cells[1].Blocks[0]).Inlines[0].Format.Bold);

        var last = Assert.IsType<Paragraph>(document.Blocks[3]);
        var link = Run(last, "link");
        Assert.Equal("https://example.com/?a=1&b={x}", link.Link?.SafeTarget);
        var image = Assert.Single(last.Inlines.OfType<InlineImage>());
        Assert.Equal(png, document.GetMedia(image.MediaPart));
        Assert.Equal((40 * 9525L, 20 * 9525L), (image.WidthEmu, image.HeightEmu));
        Assert.Contains(@"{\pict\pngblip\picw40\pich20\picwgoal600\pichgoal300 ", Encoding.ASCII.GetString(bytes));
        DocxWriterTests.AssertConsistent(DocxWriterTests.Save(document));
    }

    [Fact]
    public void Rtf_Write_ListAsTextLabel()
    {
        var bytes = Save(HtmlReader.Read("<ol><li>one</li><li>two</li></ol><p>after</p>"));

        var rtf = Encoding.ASCII.GetString(bytes);
        Assert.Contains(@"\li720\fi-360", rtf);
        Assert.DoesNotContain(@"\ls", rtf);
        var paragraphs = Load(bytes).Blocks.Cast<Paragraph>().ToList();
        Assert.Equal(["1.\tone", "2.\ttwo", "after"], paragraphs.Select(p => p.Text));
        Assert.All(paragraphs, p => Assert.Null(p.Format.Numbering));
        Assert.Equal((720, -360), (paragraphs[0].Format.IndentLeft, paragraphs[0].Format.IndentFirstLine));
    }

    [Fact]
    public void Rtf_NotRtf_ThrowsInvalidData()
    {
        Assert.Throws<InvalidDataException>(() => Load("hello"));
        Assert.Throws<InvalidDataException>(() => Load(""));
        Assert.Throws<InvalidDataException>(() => Load("{\\rt"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 \'zz}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 x} trailing"));
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new HtmlSecurityTests.EndlessStream("{\\rtf1 "u8.ToArray()), DocumentFileFormat.Rtf));
        Assert.Throws<InvalidDataException>(() => TextDocument.Load(new HtmlSecurityTests.EndlessStream("{\\rtf1 "u8.ToArray(), seekable: true), DocumentFileFormat.Rtf));
    }

    [Fact]
    public void Rtf_Read_HostileNestingAndQuadraticInput_ThrowsInvalidData()
    {
        Assert.Equal("x", Single(Load(@"{\rtf1 " + new string('{', 200) + "x" + new string('}', 200) + "}")).Text);
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + new string('{', 300) + "x" + new string('}', 300) + "}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + Repeat("{x}", 200_000) + "}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + Repeat(@"\trowd\cellx1000\pard\intbl a\cell\row\pard\par ", 30_000) + "}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + Repeat(@"\b", 5_000_000) + "}"));
    }

    [Fact]
    public void Rtf_Losses_OnlyWhatTheWriterDrops()
    {
        var gif = Convert.ToBase64String("GIF89a\u0001\0\u0001\0\0\0\0;"u8.ToArray());
        var rich = HtmlReader.Read(
            "<h1>T</h1><p style=\"text-align:center\"><span style=\"color:#FF0000;font-size:20pt\">red</span> <a href=\"https://example.com/\">l</a></p>" +
            "<table><tr><td colspan=\"2\">a</td></tr><tr><td rowspan=\"2\">b</td><td>c</td></tr><tr><td>d</td></tr></table>");

        Assert.Empty(DocumentExport.Losses(rich, DocumentFileFormat.Rtf));
        Assert.Equal([DocumentFeature.Lists], DocumentExport.Losses(HtmlReader.Read("<ol><li>a</li></ol>"), DocumentFileFormat.Rtf));
        Assert.Equal([DocumentFeature.NestedTables],
            DocumentExport.Losses(HtmlReader.Read("<table><tr><td><table><tr><td>x</td></tr></table></td></tr></table>"), DocumentFileFormat.Rtf));
        Assert.Equal([DocumentFeature.Images], DocumentExport.Losses(HtmlReader.Read($"<p><img src=\"data:image/gif;base64,{gif}\"></p>"), DocumentFileFormat.Rtf));
        Assert.Empty(DocumentExport.Losses(HtmlReader.Read($"<p><img src=\"data:image/png;base64,{Convert.ToBase64String(ObjectCommandTests.Png(2, 2))}\"></p>"),
            DocumentFileFormat.Rtf));
        var landscape = TextDocument.Create(PageSetup.A4 with { Width = 16838, Height = 11906, Orientation = PageOrientation.Landscape });
        Assert.Empty(DocumentExport.Losses(landscape, DocumentFileFormat.Rtf));
        Assert.Equal([DocumentFeature.Sections], DocumentExport.Losses(landscape, DocumentFileFormat.Html));
        Assert.Contains(@"\paperw16838\paperh11906\margl1440\margr1440\margt1440\margb1440\landscape", Encoding.ASCII.GetString(Save(landscape)));
    }

    [Fact]
    public void Rtf_Read_UnicodeRunsCountTowardsTheWorkLimit()
    {
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1\uc0 " + Repeat(@"{\u65 }", 200_000) + "}"));
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1\uc1 " + Repeat(@"{\u65?}", 200_000) + "}"));
        Assert.Equal(new string('A', 1000), Single(Load(@"{\rtf1\uc1 " + Repeat(@"\u65?", 1000) + "}")).Text);
    }

    [Fact]
    public void Rtf_Read_TabMarkerCannotBeForgedOrLeak()
    {
        var forged = Single(Load(@"{\rtf1 a\u-560?b\u-4?c\u-4?\tab d}"));
        Assert.Equal("a\uFDD0bc\td", forged.Text);
        Assert.Single(forged.Inlines.OfType<Tab>());

        var document = Load(
            @"{\rtf1{\fonttbl{\f0 Ari\tab al;}}{\info{\title T\tab itle}}\f0 " +
            @"{\field{\*\fldinst{HYPERLINK ""https://example.com/a\tab b""}}{\fldrslt{link}}} x\tab y}");
        var paragraph = Single(document);
        Assert.Equal("link x\ty", paragraph.Text);
        Assert.All(paragraph.Inlines, i => Assert.DoesNotContain('\uFFFC', (i.Link?.Target ?? "") + i.Format.Font));
        Assert.DoesNotContain('\uFFFC', document.Properties.Title ?? "");
    }

    [Fact]
    public void Rtf_Read_LimitsAcceptAtAndRejectAboveTheBoundary()
    {
        var limits = RtfLimits.Default;
        Accept(limits with { Depth = 3 }, @"{\rtf1{{x}}}");
        Reject(limits with { Depth = 3 }, @"{\rtf1{{{x}}}}");
        Accept(limits with { Tokens = 13 }, @"{\rtf1 " + Repeat("\t", 10) + "}");
        Reject(limits with { Tokens = 12 }, @"{\rtf1 " + Repeat("\t", 10) + "}");
        Accept(limits with { Paragraphs = 5 }, @"{\rtf1 " + Repeat(@"x\par ", 5) + "}");
        Reject(limits with { Paragraphs = 4 }, @"{\rtf1 " + Repeat(@"x\par ", 5) + "}");
        Accept(limits with { RunWork = 100 }, @"{\rtf1 " + Repeat("{x}", 10) + "}");
        Reject(limits with { RunWork = 99 }, @"{\rtf1 " + Repeat("{x}", 10) + "}");
        var rows = @"{\rtf1 " + Repeat(@"\trowd\cellx1000\pard\intbl a\cell\row ", 2) + "}";
        Accept(limits with { TableWork = 4 * RtfLimits.CellWork + 2 }, rows);
        Reject(limits with { TableWork = 4 * RtfLimits.CellWork + 1 }, rows);
        Accept(limits with { Html = 10_000 }, @"{\rtf1 " + Repeat("x", 1000) + "}");
        Reject(limits with { Html = 1000 }, @"{\rtf1 " + Repeat("x", 1000) + "}");
    }

    [Fact]
    public void Rtf_Read_FallbackCharsAndCodePages()
    {
        Assert.Equal("\u00FCx", Single(Load(@"{\rtf1\uc2 \u252??x}")).Text);
        Assert.Equal("\u00FC", Single(Load(@"{\rtf1\ansi\ansicpg99999 \'fc}")).Text);
        Assert.Equal("\u00FC", Single(Load(@"{\rtf1\ansi\ansicpg65000 \'fc}")).Text);
        Assert.Equal("\u00FC", Single(Load(@"{\rtf1\ansi{\cpg-5 \'fc}}")).Text);
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 a\par-b}"));
    }

    [Fact]
    public void Rtf_Read_BinaryDataIsRejected()
    {
        Assert.Single(Load(@"{\rtf1 a\bin0 b}").Blocks);
        Assert.Contains("binary", Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 \bin2 xy}")).Message);
        Assert.Contains("binary", Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1{\pict\pngblip\bin2 xy}}")).Message);
    }

    [Fact]
    public void Rtf_Read_HexEscapeHidesNoBraces()
    {
        var limits = RtfLimits.Default with { Depth = 2 };
        Assert.Contains("nested", Assert.Throws<InvalidDataException>(() => Read(limits, @"{\rtf1{\'{{x}}}")).Message);
        Assert.Contains("damaged", Assert.Throws<InvalidDataException>(() => Read(limits, @"{\rtf1{\'4}{\'4}{\'4}x}")).Message);
        Assert.Equal("\u00FCx", Single(Read(limits, @"{\rtf1{\'fc}x}")).Text);
    }

    [Fact]
    public void Rtf_Read_UnicodeFallbackSkipsCharactersLikeRtfPipe()
    {
        Assert.Equal("Ax", Single(Read(RtfLimits.Default with { Depth = 2 }, @"{\rtf1\uc1 \u65\{x}")).Text);
        Reject(RtfLimits.Default with { Depth = 1 }, @"{\rtf1\uc1 \u65\{x}");
        Assert.Equal("AAAx", Single(Read(RtfLimits.Default with { Depth = 4 }, @"{\rtf1\uc1{\u65}{\u65}{\u65}x}")).Text);
        Reject(RtfLimits.Default with { Depth = 3 }, @"{\rtf1\uc1{\u65}{\u65}{\u65}x}");
        Assert.Equal("Ax", Single(Load(@"{\rtf1\uc1 \u65\'e9x}")).Text);
        Assert.Equal("Apar x", Single(Load(@"{\rtf1\uc1 \u65\par x}")).Text);
        Assert.Equal("Ax", Single(Load("{\\rtf1\\uc1 \\u65\tx}")).Text);
    }

    [Fact]
    public void Rtf_Read_FallbackNeverHidesParagraphsFromTheLimits()
    {
        var rtf = @"{\rtf1\uc2 " + Repeat(@"\u65\par\par ", 10) + "}";

        Assert.Equal("Aar", Read(RtfLimits.Default with { Paragraphs = 10 }, rtf).Blocks.Cast<Paragraph>().First().Text);
        Reject(RtfLimits.Default with { Paragraphs = 9 }, rtf);
        Reject(RtfLimits.Default with { Paragraphs = 9 }, @"{\rtf1 " + Repeat(@"{\page }", 10) + "}");
        Reject(RtfLimits.Default with { Paragraphs = 9 }, @"{\rtf1 " + Repeat(@"{\sect }", 10) + "}");
    }

    [Fact]
    public void Rtf_Read_RunWorkCountsTheCharactersOfEveryRun()
    {
        Reject(RtfLimits.Default, @"{\rtf1 " + Repeat(@"{\chdate }", 200_000) + "}");
        Reject(RtfLimits.Default, @"{\rtf1 " + Repeat(@"{\chtime }", 200_000) + "}");
        Reject(RtfLimits.Default, @"{\rtf1 " + Repeat("{aaaaaaaaaa}", 90_000) + "}");
        Reject(RtfLimits.Default, @"{\rtf1 " + Repeat(@"\b aaaaaaaaaa\b0 ", 90_000) + "}");
        Accept(RtfLimits.Default with { RunWork = 27 },@"{\rtf1 " + Repeat("{xxx}", 3) + "}");
        Reject(RtfLimits.Default with { RunWork = 26 }, @"{\rtf1 " + Repeat("{xxx}", 3) + "}");
        Assert.Equal("a", Single(Load(@"{\rtf1 a" + Repeat(@"{\chpgn }{\column }{\softline }", 50_000) + "}")).Text);
        Assert.Equal(20_000 * 10, Single(Load(@"{\rtf1 " + Repeat(@"\chdate ", 20_000) + "}")).Text.Length);
    }

    [Fact]
    public void Rtf_Read_ObjectMarkAliasesBinVariantsAndShortInput()
    {
        foreach (var marker in new[] { @"\u-4?", @"\u65532?", @"\u131068?", @"\u-65540?" })
            Assert.Equal("ax", Single(Load(@"{\rtf1 a" + marker + "x}")).Text);
        Assert.Equal("ax", Single(Load(@"{\rtf1\uc2 a\u-4??x}")).Text);
        Assert.DoesNotContain('\uFFFC', Single(Load(@"{\rtf1\uc0 a\u-4 x}")).Text);
        foreach (var bin in new[] { @"\BIN2", @"\Bin2", @"\bin-2", @"\bin" })
            Assert.Equal("axy b", Single(Load(@"{\rtf1 a" + bin + " xy b}")).Text);
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 a\'"));
        Assert.Equal("aA", Single(Load(@"{\rtf1 a\uc5 \u65 b}")).Text);
        Assert.Equal("aA", Single(Load(@"{\rtf1 a\uc5 \u65")).Text);
    }

    [Fact]
    public void Rtf_Read_TextWordsAndUnicodeRunsCountAsText()
    {
        Assert.Throws<InvalidDataException>(() => Load(@"{\rtf1 " + Repeat(@"{\emdash }", 200_000) + "}"));
        Assert.Equal(new string('\u2014', 200_000), Single(Load(@"{\rtf1 " + Repeat(@"\emdash ", 200_000) + "}")).Text);
        Assert.Equal(100_000, Single(Load(@"{\rtf1\uc1 " + Repeat(@"\u20013?", 100_000) + @"\par}")).Text.Length);
        Assert.Equal("\u2013\u2022\u2018\u2019\u201C\u201Dx", Single(Load(@"{\rtf1 \endash\bullet\lquote\rquote\ldblquote\rdblquote x}")).Text);
    }

    [Fact]
    public void Rtf_Read_InTableIsGroupScoped()
    {
        var limits = RtfLimits.Default with { TableWork = 1 };
        Assert.Equal(["a", "b"], Read(limits, @"{\rtf1{\intbl}a\par b\par}").Blocks.Cast<Paragraph>().Select(p => p.Text));
    }

    [Fact]
    public void Rtf_Read_RawTabBlankParagraphsAndInputMarker()
    {
        var tabbed = Single(Load("{\\rtf1 a\tb}"));
        Assert.Equal("a\tb", tabbed.Text);
        Assert.Single(tabbed.Inlines.OfType<Tab>());

        Assert.Equal(["a", "", "b"], Load(@"{\rtf1 a\par\par b\par}").Blocks.Cast<Paragraph>().Select(p => p.Text));
        Assert.Equal("\u00A0", Single(Load(@"{\rtf1 \~\par}")).Text);

        var png = Convert.ToHexString(ObjectCommandTests.Png(2, 2));
        var marked = Single(Load(@"{\rtf1 a\u-4?b\u65532?c\u-4?\tab d{\pict\pngblip\picw2\pich2 " + png + "}}"));
        Assert.Equal("abc\td\uFFFC", marked.Text);
        Assert.Null(Assert.Single(marked.Inlines.OfType<InlineImage>()).AltText);
        Assert.DoesNotContain(marked.Inlines.OfType<TextRun>(), r => r.Text.Contains('\uFFFC'));
    }

    [Fact]
    public void Rtf_Write_FontTableKeyedByStrippedName()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:r><w:rPr><w:rFonts w:ascii=\"Bad;Font\" w:hAnsi=\"Bad;Font\"/></w:rPr><w:t>a</w:t></w:r>" +
            "<w:r><w:rPr><w:rFonts w:ascii=\"BadFont\" w:hAnsi=\"BadFont\"/></w:rPr><w:t>b</w:t></w:r></w:p>").Load();

        var rtf = Encoding.ASCII.GetString(Save(document));

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(rtf, "BadFont;"));
        Assert.Matches(@"\{\\f1(\\fs\d+)? a\}", rtf);
        Assert.Matches(@"\{\\f1(\\fs\d+)? b\}", rtf);
    }

    [Fact]
    public void Rtf_Read_NonBreakingSpaceRunIsKept()
    {
        Assert.Equal("a\u00A0b", Single(Load(@"{\rtf1 a{\b\~}b}")).Text);
        Assert.Equal("\u00A0", Single(Load(@"{\rtf1 \~}")).Text);
    }

    [Fact]
    public void Rtf_Write_FontNamesCellShadingAndThemeColors()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:r><w:rPr><w:rFonts w:ascii=\"Bad;Font\" w:hAnsi=\"Bad;Font\"/><w:color w:val=\"4472C4\" w:themeColor=\"accent1\" w:themeShade=\"BF\"/></w:rPr><w:t>a</w:t></w:r>" +
            "<w:r><w:rPr><w:color w:val=\"auto\"/></w:rPr><w:t>b</w:t></w:r></w:p>").Load();
        document.Theme = DocumentTheme.Read(System.Xml.Linq.XDocument.Parse("""
            <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Test"><a:themeElements>
              <a:clrScheme name="Test"><a:accent1><a:srgbClr val="808080"/></a:accent1></a:clrScheme>
              <a:fontScheme name="Test"><a:majorFont><a:latin typeface="A"/></a:majorFont><a:minorFont><a:latin typeface="B"/></a:minorFont></a:fontScheme>
            </a:themeElements></a:theme>
            """));

        var rtf = Encoding.ASCII.GetString(Save(document));

        Assert.Contains(@"\fnil BadFont;}", rtf);
        Assert.Contains(@"{\colortbl;\red96\green96\blue96;}", rtf);
        Assert.Contains(@"\cf1 a}", rtf);
        Assert.Matches(@"\{\\f0(\\fs\d+)? b\}", rtf);

        var shaded = Encoding.ASCII.GetString(Save(HtmlReader.Read("<table><tr><td style=\"background-color:#FF0000\">x</td></tr></table>")));
        Assert.Contains(@"\clbrdrr\brdrs\brdrw10\clcbpat1\cellx", shaded);
    }

    [Fact]
    public void Rtf_Write_ImageAboveCapIsDroppedAndReported()
    {
        var big = new byte[RtfWriter.MaxImageBytes + 1];
        ObjectCommandTests.Png(4, 2).CopyTo(big, 0);
        var document = TextDocument.Create();
        var position = document.PositionOf(document.Blocks[0].Id, 0);
        document.Execute(new InsertImageCommand(new DocumentRange(position, position), big, "image/png"));

        Assert.Equal([DocumentFeature.Images], DocumentExport.Losses(document, DocumentFileFormat.Rtf));
        Assert.DoesNotContain(@"\pict", Encoding.ASCII.GetString(Save(document)));
    }

    [Fact]
    public void Rtf_WriteThenRead_LinkEscapesDelimitersAndMergedCells()
    {
        var source = HtmlReader.Read(
            "<p><a href=\"https://example.com/a&quot;b\\c\">q</a> <b>1</b>2 a<i> b</i></p>" +
            "<table><tr><td colspan=\"2\">A</td><td rowspan=\"2\">R</td></tr><tr><td>B</td><td>C</td></tr></table><p>between</p>" +
            "<table><tr><td>o<table><tr><td>x</td><td>y</td></tr></table></td></tr></table>");
        Assert.Equal("https://example.com/a\"b\\c", ((Paragraph)source.Blocks[0]).Inlines[0].Link?.SafeTarget);
        Assert.Equal([DocumentFeature.NestedTables], DocumentExport.Losses(source, DocumentFileFormat.Rtf));

        var bytes = Save(source);
        var rtf = Encoding.ASCII.GetString(bytes);
        Assert.Contains(@"HYPERLINK ""https://example.com/a%22b\\\\c""", rtf);
        Assert.Contains(@"\clvmgf", rtf);
        Assert.Contains(@"\clvmrg", rtf);
        Assert.Contains(@"\pard\plain\intbl x\tab y\par", rtf);

        var document = Load(bytes);
        var first = (Paragraph)document.Blocks[0];
        Assert.Equal("q 12 a b", first.Text);
        Assert.Equal("https://example.com/a%22b\\c", Run(first, "q").Link?.SafeTarget);
        Assert.True(Run(first, "1").Format.Bold);
        Assert.Null(Run(first, "2 a").Format.Bold);
        var table = Assert.IsType<Table>(document.Blocks[1]);
        Assert.Equal([(2, "A"), (1, "R")], table.Rows[0].Cells.Select(c => (c.GridSpan, ((Paragraph)c.Blocks[0]).Text)));
        Assert.Equal(["B", "C", ""], table.Rows[1].Cells.Select(c => ((Paragraph)c.Blocks[0]).Text));
        var nested = Assert.IsType<Table>(document.Blocks[3]);
        Assert.Equal(["o", "x\ty", ""], Assert.Single(Assert.Single(nested.Rows).Cells).Blocks.Cast<Paragraph>().Select(p => p.Text));
    }

    private static void Accept(RtfLimits limits, string rtf) => Read(limits, rtf);

    private static void Reject(RtfLimits limits, string rtf) => Assert.Throws<InvalidDataException>(() => Read(limits, rtf));

    private static TextDocument Read(RtfLimits limits, string rtf) => RtfFormat.Read(new MemoryStream(Encoding.Latin1.GetBytes(rtf)), limits);

    private static string Repeat(string text, int count) => new StringBuilder(text.Length * count).Insert(0, text, count).ToString();

    private static TextRun Run(Paragraph paragraph, string text) => paragraph.Inlines.OfType<TextRun>().Single(r => r.Text == text);

    private static TextDocument Load(string rtf) => Load(Encoding.Latin1.GetBytes(rtf));

    private static TextDocument Load(byte[] bytes) => TextDocument.Load(new MemoryStream(bytes), DocumentFileFormat.Rtf);

    private static byte[] Save(TextDocument document)
    {
        using var stream = new MemoryStream();
        document.Save(stream, DocumentFileFormat.Rtf);
        return stream.ToArray();
    }

    private static Paragraph Single(TextDocument document) => Assert.IsType<Paragraph>(Assert.Single(document.Blocks));
}
