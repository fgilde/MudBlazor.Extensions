using System.Text;
using System.Xml;
using System.Xml.Linq;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public partial class DocxWriterTests
{
    private const char ObjectReplacement = (char)0xFFFC;

    private static readonly HashSet<string> DroppedMarkers =
        ["proofErr", "moveFromRangeStart", "moveFromRangeEnd", "moveToRangeStart", "moveToRangeEnd", "commentRangeStart", "commentRangeEnd"];

    [Fact]
    public void Writer_InsertRightAfterFieldSharingRun_TextInModelOrder()
    {
        var document = new DocxBuilder().Body($"<w:p>{Field}</w:p>").Load();
        var id = document.Blocks[0].Id;
        var field = ((Paragraph)document.Blocks[0]).Inlines.OfType<OpaqueInline>().Single();

        document.Execute(new ReplaceTextCommand(Range(document, id, 6, id, 6), "X"));
        var output = Save(document);

        Assert.Equal("Lead " + ObjectReplacement + "X after", ((Paragraph)document.Blocks[0]).Text);
        Assert.Equal("Lead 1X after", Text(Body(output).Element(W + "p")!));
        Assert.Same(field.Sources[^1], ((Paragraph)document.Blocks[0]).Inlines.OfType<TextRun>().First(r => r.Text.StartsWith('X')).Source);
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_InlineInsertedIntoGapOfIntactRun_RunRebuiltInModelOrder()
    {
        var document = new DocxBuilder().Body("<w:p><w:r><w:t>a</w:t><w:tab/><w:t>b</w:t></w:r></w:p>").Load();
        var paragraph = (Paragraph)document.Blocks[0];

        document.ApplyTyping(paragraph.Id, [paragraph.Inlines[0], new TextRun("X"), paragraph.Inlines[1], paragraph.Inlines[2]], DateTimeOffset.UnixEpoch);
        var output = Save(document);

        var p = Body(output).Element(W + "p")!;
        Assert.Equal(["t:a", "t:X", "tab:", "t:b"], p.Descendants().Where(e => e.Name == W + "t" || e.Name == W + "tab").Select(e => $"{e.Name.LocalName}:{e.Value}"));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_EditTextSharingFieldRun_FieldRunPropertiesUnchanged()
    {
        var source = new DocxBuilder().Body(
            "<w:p><w:r><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:instrText xml:space=\"preserve\"> PAGE </w:instrText></w:r><w:r><w:fldChar w:fldCharType=\"separate\"/></w:r>" +
            "<w:r><w:rPr><w:i/></w:rPr><w:t>1</w:t></w:r><w:r><w:rPr><w:b/></w:rPr><w:fldChar w:fldCharType=\"end\"/><w:t xml:space=\"preserve\"> outside</w:t></w:r></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 2, id, 9), "inside"));
        var output = Save(document);

        var runs = Body(output).Descendants(W + "r").ToList();
        var fieldRuns = runs.Where(r => r.Elements().Any(e => e.Name.LocalName is "fldChar" or "instrText") || r.Value == "1").ToList();
        var originals = Body(source).Descendants(W + "r").ToList();
        Assert.Equal(5, fieldRuns.Count);
        foreach (var (written, original) in fieldRuns.Zip(originals))
            Assert.True(XNode.DeepEquals(original.Element(W + "rPr"), written.Element(W + "rPr")), written.ToString());
        Assert.Equal("1 inside", Text(Body(output).Element(W + "p")!));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_RebuiltRun_KeepsUnmappedChildrenInPlace_DropsLastRenderedPageBreak()
    {
        const string body = "<w:p><w:r><w:lastRenderedPageBreak/><w:t>ab</w:t><w:delInstrText>x</w:delInstrText><w:t>cd</w:t></w:r></w:p>";
        var document = new DocxBuilder().Body(body).Load();
        var paragraph = (Paragraph)document.Blocks[0];
        document.ApplyTyping(paragraph.Id, [paragraph.Inlines[0], (TextRun)paragraph.Inlines[1] with { Text = "cD" }], DateTimeOffset.UnixEpoch);
        var other = new DocxBuilder().Body(body).Load();
        var otherId = other.Blocks[0].Id;
        other.Execute(new ReplaceTextCommand(Range(other, otherId, 0, otherId, 1), "A"));

        var runs = Body(Save(document)).Descendants(W + "r").ToList();
        var merged = Body(Save(other)).Descendants(W + "r").ToList();

        Assert.Equal(["t:ab", "delInstrText:x"], runs[0].Elements().Select(e => $"{e.Name.LocalName}:{e.Value}"));
        Assert.Equal("cD", runs[1].Value);
        Assert.Single(merged.SelectMany(r => r.Elements(W + "delInstrText")));
        Assert.DoesNotContain(runs.Concat(merged).SelectMany(r => r.Elements()), e => e.Name.LocalName == "lastRenderedPageBreak");
    }

    [Fact]
    public void Writer_FinalStateParagraph_RemovesPartnersOfDroppedMoveRangesAndDeletedComments()
    {
        var source = new DocxBuilder().Body(
            "<w:p><w:moveFromRangeStart w:id=\"5\" w:author=\"A\" w:name=\"m\"/><w:commentRangeStart w:id=\"0\"/><w:moveFrom w:id=\"6\" w:author=\"A\"><w:r><w:t>moved</w:t></w:r></w:moveFrom>" +
            "<w:r><w:t>first</w:t></w:r><w:del w:id=\"7\" w:author=\"A\"><w:r><w:commentReference w:id=\"0\"/></w:r></w:del></w:p>" +
            "<w:p><w:r><w:t>second</w:t></w:r><w:moveFromRangeEnd w:id=\"5\"/><w:commentRangeEnd w:id=\"0\"/><w:bookmarkStart w:id=\"1\" w:name=\"b\"/><w:bookmarkEnd w:id=\"1\"/></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 1), "F"));
        var output = Save(document);

        var body = Body(output);
        Assert.DoesNotContain(body.Descendants(), e => e.Name.LocalName is "moveFromRangeStart" or "moveFromRangeEnd" or "commentRangeStart" or "commentRangeEnd");
        var second = new XElement(Body(source).Elements(W + "p").Last());
        second.Elements().Where(e => e.Name.LocalName is "moveFromRangeEnd" or "commentRangeEnd").Remove();
        Assert.True(XNode.DeepEquals(second, body.Elements(W + "p").Last()));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_LinkWithEscapedTargetAndFragment_UnchangedLinkKeepsRelationship()
    {
        var source = new DocxBuilder().Link("rId20", "https://example.org/a%20b?x=1&amp;y=2#frag")
            .Body("<w:p><w:r><w:t xml:space=\"preserve\">See </w:t></w:r><w:hyperlink r:id=\"rId20\" w:history=\"1\"><w:r><w:t>link</w:t></w:r></w:hyperlink></w:p>").Build();
        var document = Load(source);
        var id = document.Blocks[0].Id;

        document.Execute(new ReplaceTextCommand(Range(document, id, 0, id, 3), "Visit"));
        var output = Save(document);

        Assert.Equal("rId20", (string)Body(output).Descendants(W + "hyperlink").Single().Attribute(R + "id"));
        Assert.True(OpcAssert.Parts(source)["/word/_rels/document.xml.rels"].AsSpan().SequenceEqual(OpcAssert.Parts(output)["/word/_rels/document.xml.rels"]));
        AssertConsistent(output);
    }

    [Fact]
    public void Writer_FormatOverToken_NestedTextBoxRunsUntouched()
    {
        var document = new DocxBuilder().Body(
            "<w:p><w:sdt><w:sdtPr><w:id w:val=\"1\"/></w:sdtPr><w:sdtContent><w:r><w:t>c</w:t></w:r><w:r><w:pict><v:shape style=\"width:50pt;height:20pt\"><v:textbox>" +
            "<w:txbxContent><w:p><w:r><w:t>inner</w:t></w:r></w:p></w:txbxContent></v:textbox></v:shape></w:pict></w:r></w:sdtContent></w:sdt></w:p>").Load();
        var id = document.Blocks[0].Id;

        document.Execute(new SetRunFormatCommand(Range(document, id, 0, id, 1), f => f with { Bold = true }, "Bold"));
        var sdt = Body(Save(document)).Descendants(W + "sdt").Single();

        Assert.All(sdt.Element(W + "sdtContent")!.Elements(W + "r"), r => Assert.NotNull(r.Element(W + "rPr")?.Element(W + "b")));
        Assert.Null(sdt.Descendants(W + "txbxContent").Single().Descendants(W + "r").Single().Element(W + "rPr"));
    }

    [Fact]
    public void Writer_DotmAsDocx_StaysMacroEnabled()
    {
        var template = new DocxBuilder { MainContentType = "application/vnd.ms-word.template.macroEnabledTemplate.main+xml" }
            .Body("<w:p><w:r><w:t>x</w:t></w:r></w:p>").Build();
        var document = TextDocument.Load(new MemoryStream(template), DocumentFileFormat.Dotx);
        Assert.True(document.HasMacros);
        Assert.True(document.IsTemplate);
        Assert.False(TextDocument.Create().HasMacros);

        var output = Save(document, false);

        Assert.Equal("application/vnd.ms-word.document.macroEnabled.main+xml", OpcPackage.Open(output).ContentType(MainPart));
        Assert.True(Load(output).HasMacros);
        Assert.False(Load(output).IsTemplate);
    }

    private static string Signature(Paragraph paragraph)
    {
        var result = new StringBuilder();
        foreach (var inline in paragraph.Inlines)
            switch (inline)
            {
                case TextRun run:
                    var text = (run.Text ?? "").Replace("\r\n", "\v").Replace('\r', '\v').Replace('\n', '\v');
                    for (var i = 0; i < text.Length; i++)
                        if (text[i] is '\t' or '\v' or '\f' || text[i] != ObjectReplacement && XmlConvert.IsXmlChar(text[i])) result.Append(text[i]);
                        else if (i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], text[i])) result.Append(text, i++, 2);
                    break;
                case Tab:
                    result.Append('\t');
                    break;
                case Break { Kind: BreakKind.Line }:
                    result.Append('\v');
                    break;
                case Break:
                    result.Append('\f');
                    break;
                case Marker marker:
                    if (marker.Source != null && !DroppedMarkers.Contains(marker.Kind)) result.Append($"<{marker.Kind}>");
                    break;
                case InlineImage { Source: null }:
                    break;
                case OpaqueInline token:
                    result.Append($"[{token.Kind}]");
                    break;
                default:
                    result.Append($"[{inline.GetType().Name}]");
                    break;
            }

        return result.ToString();
    }
}
