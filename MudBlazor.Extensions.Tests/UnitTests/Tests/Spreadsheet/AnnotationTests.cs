using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class AnnotationTests
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string XrNs = "http://schemas.microsoft.com/office/spreadsheetml/2014/revision";
    private const string CommentsType = "application/vnd.openxmlformats-officedocument.spreadsheetml.comments+xml";
    private const string VmlType = "application/vnd.openxmlformats-officedocument.vmlDrawing";
    private static readonly XNamespace M = Main;
    private static readonly XNamespace Xr = XrNs;
    private static readonly XNamespace Pkg = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Types = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XName RelId = XName.Get("id", Rel);

    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static (string Cell, string? Target, string? Location, string? Display, string? Tooltip) L(CellLink l) =>
        (l.Cell.ToA1(), l.Target, l.Location, l.Display, l.Tooltip);

    private static (string Cell, string Author, string Text) N(CellNote n) => (n.Cell.ToA1(), n.Author, n.Text);

    [Fact]
    public void Link_ExternalAndInternal_RoundTrip()
    {
        var wb = Workbook.Create();
        wb.Execute(new AddSheetCommand("Data Sheet"));
        var ws = wb.Sheets[0];
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("A1"), "https://example.com/a?b=1&c=2", null, "Example", "Open it")));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("B2"), null, "'Data Sheet'!C3", "Go", null)));
        var written = Write(wb);

        var sheet = Xml(written, "xl/worksheets/sheet1.xml").Root!;
        var links = sheet.Element(M + "hyperlinks")!.Elements(M + "hyperlink").ToList();
        Assert.Equal(2, links.Count);
        var rel = Assert.Single(Xml(written, "xl/worksheets/_rels/sheet1.xml.rels").Root!.Elements(Pkg + "Relationship"));
        Assert.Equal((Rel + "/hyperlink", "https://example.com/a?b=1&c=2", "External"),
            ((string)rel.Attribute("Type")!, (string)rel.Attribute("Target")!, (string)rel.Attribute("TargetMode")!));
        Assert.Equal(("A1", (string)rel.Attribute("Id")!, "Example", "Open it"),
            ((string)links[0].Attribute("ref")!, (string)links[0].Attribute(RelId)!, (string)links[0].Attribute("display")!, (string)links[0].Attribute("tooltip")!));
        Assert.Equal(("B2", "'Data Sheet'!C3", null), ((string)links[1].Attribute("ref")!, (string)links[1].Attribute("location")!, (string?)links[1].Attribute(RelId)));

        var back = Read(written);
        Assert.False(back.HasUnshiftedReferences);
        Assert.Equal([("A1", "https://example.com/a?b=1&c=2", null, "Example", "Open it"), ("B2", null, "'Data Sheet'!C3", "Go", null)],
            back.Sheets[0].Links.Select(L));

        var package = LinkPackage();
        var file = Read(package);
        Assert.False(file.HasUnshiftedReferences);
        var fs = file.Sheets[0];
        Assert.Equal([("A1", "https://example.com/", null, "Site", null), ("A2", null, "Sheet2!B3", "Go", "tip")], fs.Links.Select(L));
        Assert.Equal(Entry(package, "xl/worksheets/sheet1.xml"), Entry(Write(file), "xl/worksheets/sheet1.xml"));

        file.Execute(new InsertRowsCommand(fs, 0, 1));
        var shifted = Write(file);
        Assert.Equal(Entry(package, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(shifted, "xl/worksheets/_rels/sheet1.xml.rels"));
        var patched = Xml(shifted, "xl/worksheets/sheet1.xml").Root!.Element(M + "hyperlinks")!.Elements().ToList();
        Assert.Equal(("A2", "rId1", "{L1}"), ((string)patched[0].Attribute("ref")!, (string)patched[0].Attribute(RelId)!, (string)patched[0].Attribute(Xr + "uid")!));
        Assert.Equal(("A3", "Sheet2!B3", "{L2}"), ((string)patched[1].Attribute("ref")!, (string)patched[1].Attribute("location")!, (string)patched[1].Attribute(Xr + "uid")!));

        file.Execute(new SetLinkCommand(fs, fs.Links[0] with { Target = "https://other.example/" }));
        var retargeted = Write(file);
        var rels = Xml(retargeted, "xl/worksheets/_rels/sheet1.xml.rels").Root!.Elements(Pkg + "Relationship").ToList();
        var newRel = Assert.Single(rels, r => (string)r.Attribute("Type")! == Rel + "/hyperlink");
        Assert.Equal("https://other.example/", (string)newRel.Attribute("Target")!);
        Assert.NotEqual("rId1", (string)newRel.Attribute("Id")!);
        Assert.Contains(rels, r => (string)r.Attribute("Id")! == "rId2");
        var first = Xml(retargeted, "xl/worksheets/sheet1.xml").Root!.Element(M + "hyperlinks")!.Elements().First();
        Assert.Equal(((string)newRel.Attribute("Id")!, "{L1}"), ((string)first.Attribute(RelId)!, (string)first.Attribute(Xr + "uid")!));
    }

    [Fact]
    public void Link_InternalLocation_ShiftsAndRenames()
    {
        var wb = Workbook.Create();
        wb.Execute(new AddSheetCommand("Data"));
        var ws = wb.Sheets[0];
        var data = wb.Sheets[1];
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("A1"), null, "Data!B5", null, null)));
        wb.Execute(new SetLinkCommand(data, new CellLink(R("A1"), null, "C4", null, null)));

        wb.Execute(new InsertRowsCommand(data, 1, 2));
        Assert.Equal("Data!B7", ws.Links[0].Location);
        Assert.Equal("C6", data.Links[0].Location);
        wb.Execute(new InsertColumnsCommand(data, 0, 1));
        Assert.Equal(("Data!C7", "D6"), (ws.Links[0].Location, data.Links[0].Location));
        Assert.Equal(R("B1"), data.Links[0].Cell);
        wb.Execute(new RenameSheetCommand(data, "Q1 Data"));
        Assert.Equal("'Q1 Data'!C7", ws.Links[0].Location);
        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        Assert.Equal(("A2", "'Q1 Data'!C7"), (ws.Links[0].Cell.ToA1(), ws.Links[0].Location));

        for (var i = 0; i < 4; i++) wb.Undo.Undo();
        Assert.Equal(("A1", "Data!B5"), (ws.Links[0].Cell.ToA1(), ws.Links[0].Location));
        Assert.Equal(("A1", "C4"), (data.Links[0].Cell.ToA1(), data.Links[0].Location));

        wb.Execute(new RemoveLinkCommand(ws, R("A1")));
        Assert.Empty(ws.Links);
        Assert.Throws<ArgumentException>(() => wb.Execute(new RemoveLinkCommand(ws, R("A1"))));
        wb.Undo.Undo();
        Assert.Single(ws.Links);
    }

    [Fact]
    public void Note_AddEditRemove_Undo()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var ranges = new List<RangeChange>();
        wb.Changed += (_, e) => ranges.AddRange(e.FormattedRanges);

        wb.Execute(new SetNoteCommand(ws, new CellNote(R("B2"), "Ann", "Hello")));
        Assert.Equal([("B2", "Ann", "Hello")], ws.Notes.Select(N));
        Assert.Contains(ranges, r => r.Sheet == ws && r.Range.Contains(R("B2")));
        wb.Execute(new SetNoteCommand(ws, ws.Notes[0] with { Text = "Edited" }));
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("C3"), "Bob", "Other")));
        Assert.Equal([("B2", "Ann", "Edited"), ("C3", "Bob", "Other")], ws.Notes.Select(N));
        wb.Execute(new RemoveNoteCommand(ws, R("B2")));
        Assert.Equal([("C3", "Bob", "Other")], ws.Notes.Select(N));
        Assert.Throws<ArgumentException>(() => wb.Execute(new RemoveNoteCommand(ws, R("B2"))));

        wb.Undo.Undo();
        Assert.Equal(("B2", "Ann", "Edited"), N(ws.Notes.Single(n => n.Cell == R("B2"))));
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal([("B2", "Ann", "Hello")], ws.Notes.Select(N));
        wb.Undo.Undo();
        Assert.Empty(ws.Notes);
        wb.Undo.Redo();
        Assert.Equal([("B2", "Ann", "Hello")], ws.Notes.Select(N));
    }

    [Fact]
    public void Notes_RoundTrip_ExistingVmlShapesPreserved()
    {
        var package = NotePackage(checkbox: true);
        var wb = Read(package);
        var ws = wb.Sheets[0];
        Assert.False(ws.NotesReadOnly);
        Assert.Equal([("B2", "Ann", "Ann:\nfirst"), ("D5", "Bob", "Bob: second")], ws.Notes.Select(N));
        Assert.True(wb.HasUnshiftedReferences);
        var same = Write(wb);
        foreach (var part in (string[])["xl/comments1.xml", "xl/drawings/vmlDrawing1.vml", "xl/worksheets/sheet1.xml", "xl/worksheets/_rels/sheet1.xml.rels"])
            Assert.Equal(Entry(package, part), Entry(same, part));

        wb.Execute(new SetNoteCommand(ws, ws.Notes[0] with { Text = "changed" }));
        wb.Execute(new InsertRowsCommand(ws, 3, 1));
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("F1"), "Me", "new")));
        var written = Write(wb);

        Assert.Equal(Entry(package, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(written, "xl/worksheets/_rels/sheet1.xml.rels"));
        Assert.Equal("rId3", (string)Xml(written, "xl/worksheets/sheet1.xml").Root!.Element(M + "legacyDrawing")!.Attribute(RelId)!);
        var comments = Xml(written, "xl/comments1.xml").Root!;
        Assert.Equal(["Ann", "Bob", "Me"], comments.Element(M + "authors")!.Elements().Select(a => a.Value));
        var list = comments.Element(M + "commentList")!.Elements(M + "comment").ToDictionary(c => (string)c.Attribute("ref")!);
        Assert.Equal(["F1", "B2", "D6"], list.Keys);
        Assert.Equal(("{N1}", "changed"), ((string)list["B2"].Attribute(Xr + "uid")!, list["B2"].Element(M + "text")!.Value));
        Assert.Empty(list["B2"].Element(M + "text")!.Elements(M + "r"));
        var original = XDocument.Parse(Comments).Root!.Element(M + "commentList")!.Elements().Last();
        Assert.Equal(("{N2}", "1"), ((string)list["D6"].Attribute(Xr + "uid")!, (string)list["D6"].Attribute("authorId")!));
        Assert.True(XNode.DeepEquals(original.Element(M + "text"), list["D6"].Element(M + "text")));
        Assert.Equal("2", (string)list["F1"].Attribute("authorId")!);
        Assert.NotNull(comments.Element(M + "extLst"));

        var vml = Encoding.Latin1.GetString(Entry(written, "xl/drawings/vmlDrawing1.vml"));
        Assert.Contains(CheckboxShape, vml);
        Assert.Contains(NoteB2Shape, vml);
        Assert.DoesNotContain(NoteD5Shape, vml);
        Assert.Contains(NoteD5Shape.Replace("<x:Row>4</x:Row>", "<x:Row>5</x:Row>").Replace("4, 15, 3, 10, 6, 15, 7, 4", "4, 15, 4, 10, 6, 15, 8, 4"), vml);
        Assert.StartsWith(Vml(checkbox: true)[..Vml(checkbox: true).IndexOf("<v:shape ", StringComparison.Ordinal)], vml);
        Assert.Contains("id=\"_x0000_s1028\"", vml);
        Assert.Contains("<x:Row>0</x:Row>", vml);
        Assert.Contains("<x:Column>5</x:Column>", vml);
        Assert.Equal(5, vml.Split("<x:ClientData ObjectType=").Length);

        var back = Read(written).Sheets[0];
        Assert.Equal([("F1", "Me", "new"), ("B2", "Ann", "changed"), ("D6", "Bob", "Bob: second")], back.Notes.OrderBy(n => n.Cell.Row).ThenBy(n => n.Cell.Column).Select(N));
    }

    [Fact]
    public void ThreadedComments_KeepNotesReadOnly()
    {
        var package = NotePackage(threaded: true);
        var wb = Read(package);
        var ws = wb.Sheets[0];
        Assert.True(ws.NotesReadOnly);
        Assert.Equal(2, ws.Notes.Count);
        Assert.True(wb.HasUnshiftedReferences);
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new SetNoteCommand(ws, new CellNote(R("A1"), "x", "y"))));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new RemoveNoteCommand(ws, R("B2"))));

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        var written = Write(wb);
        foreach (var part in (string[])["xl/comments1.xml", "xl/drawings/vmlDrawing1.vml", "xl/threadedComments/threadedComment1.xml", "xl/worksheets/_rels/sheet1.xml.rels"])
            Assert.Equal(Entry(package, part), Entry(written, part));
        Assert.Equal("rId3", (string)Xml(written, "xl/worksheets/sheet1.xml").Root!.Element(M + "legacyDrawing")!.Attribute(RelId)!);
    }

    [Fact]
    public void DeleteRowWithNote_RemovesNote()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("B3"), "Ann", "gone")));
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("B5"), "Ann", "stays")));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("C3"), "https://example.com/", null, null, null)));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("C6"), "https://example.com/", null, null, null)));

        wb.Execute(new DeleteRowsCommand(ws, 2, 1));
        Assert.Equal([("B4", "Ann", "stays")], ws.Notes.Select(N));
        Assert.Equal([R("C5")], ws.Links.Select(l => l.Cell));
        wb.Execute(new DeleteColumnsCommand(ws, 1, 1));
        Assert.Empty(ws.Notes);
        Assert.Equal([R("B5")], ws.Links.Select(l => l.Cell));

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal([("B3", "Ann", "gone"), ("B5", "Ann", "stays")], ws.Notes.OrderBy(n => n.Cell.Row).Select(N));
        Assert.Equal([R("C3"), R("C6")], ws.Links.Select(l => l.Cell).OrderBy(c => c.Row));
    }

    [Fact]
    public void NewNote_CreatesCommentsAndVmlParts()
    {
        var wb = Read(LinkPackage());
        var ws = wb.Sheets[0];
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("C4"), "Ann", "Line 1\nLine <2> & more")));
        var written = Write(wb);

        var types = Xml(written, "[Content_Types].xml").Root!;
        Assert.Contains(types.Elements(Types + "Override"), t => (string)t.Attribute("PartName")! == "/xl/comments1.xml" && (string)t.Attribute("ContentType")! == CommentsType);
        Assert.Contains(types.Elements(Types + "Default"), t => (string)t.Attribute("Extension")! == "vml" && (string)t.Attribute("ContentType")! == VmlType);
        var rels = Xml(written, "xl/worksheets/_rels/sheet1.xml.rels").Root!.Elements(Pkg + "Relationship").ToList();
        Assert.Equal(["rId1", "rId2", "rId3", "rId4"], rels.Select(r => (string)r.Attribute("Id")!));
        var comments = rels.Single(r => (string)r.Attribute("Type")! == Rel + "/comments");
        var vmlRel = rels.Single(r => (string)r.Attribute("Type")! == Rel + "/vmlDrawing");
        Assert.Equal(("../comments1.xml", "../drawings/vmlDrawing1.vml"), ((string)comments.Attribute("Target")!, (string)vmlRel.Attribute("Target")!));
        var sheet = Xml(written, "xl/worksheets/sheet1.xml").Root!;
        Assert.Equal((string)vmlRel.Attribute("Id")!, (string)sheet.Element(M + "legacyDrawing")!.Attribute(RelId)!);
        Assert.Equal(["sheetData", "hyperlinks", "pageMargins", "legacyDrawing"], sheet.Elements().Select(e => e.Name.LocalName).SkipWhile(n => n != "sheetData"));

        var vml = Encoding.Latin1.GetString(Entry(written, "xl/drawings/vmlDrawing1.vml"));
        Assert.Contains("o:spt=\"202\"", vml);
        Assert.Contains("<x:ClientData ObjectType=\"Note\">", vml);
        Assert.Contains("<x:Row>3</x:Row>", vml);
        Assert.Contains("<x:Column>2</x:Column>", vml);
        var note = Xml(written, "xl/comments1.xml").Root!.Element(M + "commentList")!.Element(M + "comment")!;
        Assert.Equal(("C4", "0", "Line 1\nLine <2> & more"), ((string)note.Attribute("ref")!, (string)note.Attribute("authorId")!, note.Element(M + "text")!.Value));

        var back = Read(written);
        Assert.False(back.HasUnshiftedReferences);
        Assert.Equal([("C4", "Ann", "Line 1\nLine <2> & more")], back.Sheets[0].Notes.Select(N));

        back.Execute(new RemoveNoteCommand(back.Sheets[0], R("C4")));
        var removed = Write(back);
        var names = Entries(removed);
        Assert.DoesNotContain("xl/comments1.xml", names);
        Assert.DoesNotContain("xl/drawings/vmlDrawing1.vml", names);
        Assert.Null(Xml(removed, "xl/worksheets/sheet1.xml").Root!.Element(M + "legacyDrawing"));
        Assert.Equal(2, Xml(removed, "xl/worksheets/_rels/sheet1.xml.rels").Root!.Elements().Count());
        Assert.DoesNotContain(Xml(removed, "[Content_Types].xml").Root!.Elements(Types + "Override"), t => (string)t.Attribute("PartName")! == "/xl/comments1.xml");

        var fresh = Workbook.Create();
        fresh.Execute(new SetNoteCommand(fresh.Sheets[0], new CellNote(R("A1"), "Ann", "x")));
        Assert.Equal([("A1", "Ann", "x")], Read(Write(fresh)).Sheets[0].Notes.Select(N));
    }

    [Fact]
    public void UnsafeScheme_IsNotNavigable()
    {
        static CellLink Ext(string target) => new(new CellRef(0, 0), target, null, null, null);
        foreach (var safe in (string[])["http://example.com", "https://example.com/x?y=1", "HTTPS://EXAMPLE.COM", "mailto:someone@example.com"])
            Assert.True(Ext(safe).IsSafe, safe);
        foreach (var unsafeTarget in (string[])["javascript:alert(1)", "JavaScript:alert(1)", " javascript:alert(1)", "java\tscript:alert(1)", "vbscript:x",
                     "data:text/html,<script>alert(1)</script>", "file:///C:/Windows/System32/calc.exe", "\\\\server\\share\\x.exe", "other.xlsx", ""])
            Assert.False(Ext(unsafeTarget).IsSafe, unsafeTarget);
        Assert.False(Ext("&#106;avascript:alert(1)").IsSafe);
        static CellLink Internal(string location) => new(new CellRef(0, 0), null, location, null, null);
        foreach (var safe in (string[])["Sheet2!A1", "'My Sheet'!A1:B2", "A1:B2", "Sheet2!AB:AC", "1:3", "XFD1:XFD2", "MyName", "'R&D'!C3"])
            Assert.True(Internal(safe).IsSafe, safe);
        foreach (var unsafeLocation in (string[])["javascript:alert(1)", "JaVaScRiPt:alert(1)", " javascript:alert(1)", "&#106;avascript:alert(1)",
                     "&#x6A;avascript:alert(1)", "https://example.com", "C:\\Windows\\calc.exe", "tel:sms", "sms:abc", "TEL:XYZ", "AB:AC"])
            Assert.False(Internal(unsafeLocation).IsSafe, unsafeLocation);
    }

    [Fact]
    public void Link_InsideRangedLink_KeepsRangeAndWins()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("A1"), "https://range.example/", null, null, null) { Span = (2, 2) }));
        wb.Execute(new SetLinkCommand(ws, new CellLink(R("B2"), "https://cell.example/", null, null, null)));

        Assert.Equal(["A1:C3", "B2"], ws.Links.Select(l => l.Area.ToA1()));
        Assert.Equal("https://cell.example/", ws.LinkAt(R("B2"))!.Target);
        Assert.Equal("https://range.example/", ws.LinkAt(R("C3"))!.Target);
        Assert.Null(ws.LinkAt(R("D4")));

        wb.Execute(new SetLinkCommand(ws, ws.LinkAt(R("C3"))! with { Target = "https://edited.example/" }));
        Assert.Equal(["A1:C3", "B2"], ws.Links.Select(l => l.Area.ToA1()));
        Assert.Equal("https://edited.example/", ws.Links[0].Target);

        wb.Execute(new RemoveLinkCommand(ws, R("B2")));
        Assert.Equal("https://edited.example/", ws.LinkAt(R("B2"))!.Target);

        var back = Read(Write(wb)).Sheets[0];
        Assert.Equal(["A1:C3"], back.Links.Select(l => l.Area.ToA1()));
    }

    [Fact]
    public void Links_UnparsableRefAndSharedRel_Kept()
    {
        var package = LinkPackage(extra: "<hyperlink ref=\"A1\" r:id=\"rId1\" display=\"Twin\"/><hyperlink ref=\"nonsense\" r:id=\"rId3\" xr:uid=\"{L3}\"/>",
            extraRels: $"<Relationship Id=\"rId3\" Type=\"{Rel}/hyperlink\" Target=\"https://kept.example/\" TargetMode=\"External\"/>");
        var wb = Read(package);
        var ws = wb.Sheets[0];
        Assert.Equal(3, ws.Links.Count);

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        var written = Write(wb);
        var links = Xml(written, "xl/worksheets/sheet1.xml").Root!.Element(M + "hyperlinks")!.Elements().ToList();
        Assert.Equal([("A2", "rId1"), ("A3", null), ("A2", "rId1"), ("nonsense", "rId3")], links.Select(l => ((string)l.Attribute("ref")!, (string?)l.Attribute(RelId))));
        Assert.Equal("{L3}", (string)links[3].Attribute(Xr + "uid")!);
        Assert.Equal(Entry(package, "xl/worksheets/_rels/sheet1.xml.rels"), Entry(written, "xl/worksheets/_rels/sheet1.xml.rels"));
        Assert.Equal(["https://example.com/", "https://example.com/"], Read(written).Sheets[0].Links.Where(l => l.Target != null).Select(l => l.Target));
    }

    [Fact]
    public void NewNotes_OnTwoSheets_GetDistinctIdMapsAndShapeIds()
    {
        var wb = Workbook.Create();
        wb.Execute(new AddSheetCommand("Second"));
        wb.Execute(new SetNoteCommand(wb.Sheets[0], new CellNote(R("A1"), "Ann", "one")));
        wb.Execute(new SetNoteCommand(wb.Sheets[1], new CellNote(R("A1"), "Ann", "two")));
        var written = Write(wb);

        var vmls = Entries(written).Where(n => n.EndsWith(".vml", StringComparison.Ordinal)).Select(n => Encoding.Latin1.GetString(Entry(written, n))).ToList();
        Assert.Equal(2, vmls.Count);
        static string Match(string vml, string pattern) => System.Text.RegularExpressions.Regex.Match(vml, pattern).Groups[1].Value;
        Assert.NotEqual(Match(vmls[0], "data=\"(\\d+)\""), Match(vmls[1], "data=\"(\\d+)\""));
        Assert.NotEqual(Match(vmls[0], "id=\"(_x0000_s\\d+)\""), Match(vmls[1], "id=\"(_x0000_s\\d+)\""));
        var back = Read(written);
        Assert.Equal(["one", "two"], back.Sheets.Select(s => s.Notes.Single().Text));
    }

    [Fact]
    public void Vml_WithByteOrderMark_KeepsBytes()
    {
        var package = NotePackage(bom: true);
        var wb = Read(package);
        var ws = wb.Sheets[0];
        Assert.Equal(2, ws.Notes.Count);
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("F1"), "Me", "new")));
        var vml = Entry(Write(wb), "xl/drawings/vmlDrawing1.vml");

        var original = Entry(package, "xl/drawings/vmlDrawing1.vml");
        var end = Array.LastIndexOf(original, (byte)'<');
        Assert.Equal(original[..end], vml[..end]);
        Assert.Equal([0xEF, 0xBB, 0xBF], vml[..3]);
        Assert.Contains("\u00e4\u00f6", Encoding.UTF8.GetString(vml));
    }

    [Fact]
    public void Vml_WithoutNoteShapeType_GetsItBeforeShapes()
    {
        var wb = Read(NotePackage(shapeType: false));
        var ws = wb.Sheets[0];
        wb.Execute(new SetNoteCommand(ws, new CellNote(R("F1"), "Me", "new")));
        var vml = Encoding.Latin1.GetString(Entry(Write(wb), "xl/drawings/vmlDrawing1.vml"));

        var type = vml.IndexOf("<v:shapetype id=\"_x0000_t202\"", StringComparison.Ordinal);
        Assert.InRange(type, 0, vml.IndexOf("<v:shape ", StringComparison.Ordinal));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(vml, "<v:shapetype "));
    }

    private const string Comments = $$"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><comments xmlns="{{Main}}" xmlns:xr="{{XrNs}}"><authors><author>Ann</author><author>Bob</author></authors><commentList><comment ref="B2" authorId="0" shapeId="0" xr:uid="{N1}"><text><r><rPr><b/><sz val="9"/></rPr><t>Ann:</t></r><r><rPr><sz val="9"/></rPr><t xml:space="preserve">&#10;first</t></r></text></comment><comment ref="D5" authorId="1" shapeId="0" xr:uid="{N2}"><text><r><rPr><b/></rPr><t>Bob:</t></r><r><t xml:space="preserve"> second</t></r></text></comment></commentList><extLst><ext uri="{X}"/></extLst></comments>""";

    private const string NoteB2Shape = "<v:shape id=\"_x0000_s1025\" type=\"#_x0000_t202\" style='position:absolute;\r\n  margin-left:59.25pt;margin-top:1.5pt;width:108pt;height:59.25pt;z-index:1;\r\n  visibility:hidden' fillcolor=\"#ffffe1\" o:insetmode=\"auto\">\r\n  <v:fill color2=\"#ffffe1\"/>\r\n  <v:textbox style='mso-direction-alt:auto'>\r\n   <div style='text-align:left'><br></div>\r\n  </v:textbox>\r\n  <x:ClientData ObjectType=\"Note\">\r\n   <x:MoveWithCells/>\r\n   <x:Anchor>\r\n    2, 15, 0, 10, 4, 15, 4, 4</x:Anchor>\r\n   <x:AutoFill>False</x:AutoFill>\r\n   <x:Row>1</x:Row>\r\n   <x:Column>1</x:Column>\r\n  </x:ClientData>\r\n </v:shape>";

    private const string CheckboxShape = "<v:shape id=\"_x0000_s1026\" type=\"#_x0000_t201\" style='position:absolute'>\r\n  <x:ClientData ObjectType=\"Checkbox\"><x:Anchor>5, 0, 8, 0, 6, 0, 9, 0</x:Anchor><x:Checked>1</x:Checked></x:ClientData>\r\n </v:shape>";

    private const string NoteD5Shape = "<v:shape id=\"_x0000_s1027\" type=\"#_x0000_t202\" style='position:absolute;visibility:hidden' fillcolor=\"#ffffe1\">\r\n  <x:ClientData ObjectType=\"Note\">\r\n   <x:Anchor>\r\n    4, 15, 3, 10, 6, 15, 7, 4</x:Anchor>\r\n   <x:Row>4</x:Row>\r\n   <x:Column>3</x:Column>\r\n  </x:ClientData>\r\n </v:shape>";

    private static string Vml(bool checkbox, bool shapeType = true) =>
        "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\"\r\n xmlns:o=\"urn:schemas-microsoft-com:office:office\"\r\n xmlns:x=\"urn:schemas-microsoft-com:office:excel\">\r\n"
        + " <o:shapelayout v:ext=\"edit\">\r\n  <o:idmap v:ext=\"edit\" data=\"1\"/>\r\n </o:shapelayout>"
        + (shapeType ? "<v:shapetype id=\"_x0000_t202\" coordsize=\"21600,21600\" o:spt=\"202\"\r\n"
                       + "  path=\"m,l,21600r21600,l21600,xe\">\r\n  <v:stroke joinstyle=\"miter\"/>\r\n  <v:path gradientshapeok=\"t\" o:connecttype=\"rect\"/>\r\n </v:shapetype>" : "")
        + NoteB2Shape + (checkbox ? CheckboxShape : "") + NoteD5Shape + "</xml>";

    private static byte[] NotePackage(bool checkbox = false, bool threaded = false, bool bom = false, bool shapeType = true)
    {
        var vml = bom ? [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(Vml(checkbox).Replace("</xml>", "<!-- äö --></xml>"))]
            : Encoding.Latin1.GetBytes(Vml(checkbox, shapeType));
        var sheet = XlsxReadTests.Sheet("<row r=\"2\"><c r=\"B2\"><v>1</v></c></row>", "",
            "<pageMargins left=\"0.7\" right=\"0.7\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/><legacyDrawing r:id=\"rId3\"/>");
        var rels = $"<Relationship Id=\"rId2\" Type=\"{Rel}/comments\" Target=\"../comments1.xml\"/><Relationship Id=\"rId3\" Type=\"{Rel}/vmlDrawing\" Target=\"../drawings/vmlDrawing1.vml\"/>"
                   + (threaded ? "<Relationship Id=\"rId1\" Type=\"http://schemas.microsoft.com/office/2017/10/relationships/threadedComment\" Target=\"../threadedComments/threadedComment1.xml\"/>" : "");
        var types = $"<Override PartName=\"/xl/comments1.xml\" ContentType=\"{CommentsType}\"/><Default Extension=\"vml\" ContentType=\"{VmlType}\"/>"
                    + (threaded ? "<Override PartName=\"/xl/threadedComments/threadedComment1.xml\" ContentType=\"application/vnd.ms-excel.threadedcomments+xml\"/>" : "");
        return With(XlsxReadTests.Package([sheet]), types, ("xl/worksheets/_rels/sheet1.xml.rels", RelsXml(rels)), ("xl/comments1.xml", Encoding.UTF8.GetBytes(Comments)),
            ("xl/drawings/vmlDrawing1.vml", vml),
            ("xl/threadedComments/threadedComment1.xml", threaded ? Encoding.UTF8.GetBytes("<ThreadedComments xmlns=\"http://schemas.microsoft.com/office/spreadsheetml/2018/threadedcomments\"/>") : null));
    }

    private static byte[] LinkPackage(string extra = "", string extraRels = "")
    {
        var sheet = XlsxReadTests.Sheet("<row r=\"1\"><c r=\"A1\"><v>1</v></c></row>", "",
            "<hyperlinks><hyperlink ref=\"A1\" r:id=\"rId1\" display=\"Site\" xr:uid=\"{L1}\"/><hyperlink ref=\"A2\" location=\"Sheet2!B3\" display=\"Go\" tooltip=\"tip\" xr:uid=\"{L2}\"/>"
            + extra + "</hyperlinks><pageMargins left=\"0.7\" right=\"0.7\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\"/>", $" xmlns:xr=\"{XrNs}\"");
        var rels = $"<Relationship Id=\"rId1\" Type=\"{Rel}/hyperlink\" Target=\"https://example.com/\" TargetMode=\"External\"/><Relationship Id=\"rId2\" Type=\"{Rel}/printerSettings\" Target=\"../printerSettings/printerSettings1.bin\"/>"
                   + extraRels;
        return With(XlsxReadTests.Package([sheet, XlsxReadTests.Sheet("")]), "<Default Extension=\"bin\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.printerSettings\"/>",
            ("xl/worksheets/_rels/sheet1.xml.rels", RelsXml(rels)), ("xl/printerSettings/printerSettings1.bin", new byte[] { 1, 2, 3 }));
    }

    private static byte[] RelsXml(string rels) =>
        Encoding.UTF8.GetBytes($"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">{rels}</Relationships>");

    private static byte[] With(byte[] package, string types, params (string Name, byte[]? Content)[] entries)
    {
        var ms = new MemoryStream();
        ms.Write(package);
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
        {
            var typesEntry = zip.GetEntry("[Content_Types].xml")!;
            string xml;
            using (var reader = new StreamReader(typesEntry.Open())) xml = reader.ReadToEnd();
            typesEntry.Delete();
            using (var s = zip.CreateEntry("[Content_Types].xml").Open()) s.Write(Encoding.UTF8.GetBytes(xml.Replace("</Types>", types + "</Types>")));
            foreach (var (name, content) in entries.Where(e => e.Content != null))
            {
                zip.GetEntry(name)?.Delete();
                using var s = zip.CreateEntry(name).Open();
                s.Write(content);
            }
        }

        return ms.ToArray();
    }

    private static Workbook Read(byte[] package) => XlsxReader.Read(new MemoryStream(package));

    private static byte[] Write(Workbook wb)
    {
        var ms = new MemoryStream();
        XlsxWriter.Write(wb, ms);
        return ms.ToArray();
    }

    private static List<string> Entries(byte[] zip)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        return archive.Entries.Select(e => e.FullName).ToList();
    }

    private static byte[] Entry(byte[] zip, string name)
    {
        using var archive = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
        using var s = archive.GetEntry(name)!.Open();
        var ms = new MemoryStream();
        s.CopyTo(ms);
        return ms.ToArray();
    }

    private static XDocument Xml(byte[] zip, string name) => XDocument.Load(new MemoryStream(Entry(zip, name)));
}
