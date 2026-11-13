using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook.NotebookBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class NotebookCommandTests
{
    private static readonly Dictionary<string, Func<Notebook, IDocumentCommand<Notebook>>> Cases = new()
    {
        ["AddSection"] = n => new AddSectionCommand(1, "New", "#123456"),
        ["RenameSection"] = n => new RenameSectionCommand(n.Sections[0], "Renamed"),
        ["SetSectionColor"] = n => new SetSectionColorCommand(n.Sections[1], "#ABCDEF"),
        ["DeleteSection"] = n => new DeleteSectionCommand(n.Sections[0]),
        ["MoveSection"] = n => new MoveSectionCommand(n.Sections[0], 2),
        ["AddPage"] = n => new AddPageCommand(n.Sections[1], 1, "New"),
        ["RenamePage"] = n => new RenamePageCommand(First(n), "Renamed"),
        ["DeletePagesEmptyingSection"] = n => new DeletePagesCommand([n.Sections[1].Pages[0], First(n)]),
        ["DeletePages"] = n => new DeletePagesCommand([n.Sections[1].Pages[1]]),
        ["MovePages"] = n => new MovePagesCommand([n.Sections[1].Pages[1], First(n), n.Sections[1].Pages[0]], n.Sections[1], 1),
        ["SetPageLevel"] = n => new SetPageLevelCommand([First(n), n.Sections[1].Pages[0]], 2),
        ["DuplicatePages"] = n => new DuplicatePagesCommand([n.Sections[1].Pages[1], First(n)]),
        ["AddElement"] = n => new AddElementCommand(First(n), new ShapeElement { Id = "new", X = 1, Y = 2, Width = 3, Height = 4, Kind = NoteShapeKind.Ellipse }),
        ["DeleteElements"] = n => new DeleteElementsCommand(First(n), ["table", "ink"]),
        ["MoveElements"] = n => new MoveElementsCommand(First(n), new Dictionary<string, (double X, double Y)> { ["text"] = (1, 2), ["ink"] = (3, 4) }),
        ["ResizeElements"] = n => new ResizeElementsCommand(First(n), new Dictionary<string, NoteBounds>
        {
            ["ink"] = new(0, 0, 60, 80), ["table"] = new(200, 300, 440, 0), ["shape"] = new(1, 1, 10, 10)
        }),
        ["ReorderToFront"] = n => new ReorderElementsCommand(First(n), ["ink", "text"], true),
        ["ReorderToBack"] = n => new ReorderElementsCommand(First(n), ["table"], false),
        ["DuplicateElements"] = n => new DuplicateElementsCommand(First(n), ["shape", "text"], 10),
        ["AddImage"] = n => new AddImageCommand(First(n), PngOfSize(1200, 600), "image/png", 5, 6, 600),
        ["SetText"] = n => new SetTextCommand(First(n), "text", [Paragraph("Hello")]),
        ["SetTextEmpty"] = n => new SetTextCommand(First(n), "text", [Paragraph("")]),
        ["SetShapeStyle"] = n => new SetShapeStyleCommand(First(n), ["shape", "ink", "text"], "#FF00FF", 4, ""),
        ["AddStroke"] = n => new AddStrokeCommand(First(n), [new InkPoint(10, 10, 0.5), new InkPoint(20, 30, 1)], "#FF0000", 2, InkTool.Pen),
        ["EraseStrokes"] = n => new EraseStrokesCommand(First(n), ["ink"]),
        ["AddTable"] = n => new AddTableCommand(First(n), 2, 3, 10, 10),
        ["SetTableCell"] = n => new SetTableCellCommand(First(n), "table", 1, 0, [Paragraph("x")]),
        ["InsertTableRows"] = n => new InsertTableRowsCommand(First(n), "table", 1, 2),
        ["DeleteTableRows"] = n => new DeleteTableRowsCommand(First(n), "table", 0, 1),
        ["InsertTableColumns"] = n => new InsertTableColumnsCommand(First(n), "table", 2, 1),
        ["DeleteTableColumns"] = n => new DeleteTableColumnsCommand(First(n), "table", 0, 1),
        ["SetTableColumnWidths"] = n => new SetTableColumnWidthsCommand(First(n), "table", [50, 60]),
        ["SetTableHeaderRow"] = n => new SetTableHeaderRowCommand(First(n), "table", false)
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    private static NotebookPage First(Notebook notebook) => notebook.Sections[0].Pages[0];

    private static NoteParagraph Paragraph(string text, NoteListKind list = NoteListKind.None, int indent = 0) =>
        new() { Runs = [new NoteRun(text)], List = list, Indent = indent };

    private static byte[] PngOfSize(int width, int height)
    {
        var png = new byte[24];
        Png.AsSpan(0, 8).CopyTo(png);
        "IHDR"u8.CopyTo(png.AsSpan(12));
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), width);
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20), height);
        return png;
    }

    internal static string Snapshot(Notebook notebook)
    {
        var sb = new StringBuilder();
        sb.AppendLine(JsonSerializer.Serialize(NotebookJson.ToDto(notebook), NotebookJsonContext.Default.ManifestDto));
        sb.AppendLine("sections " + string.Join(",", notebook.Sections.Select(Ref)));
        foreach (var page in notebook.Sections.SelectMany(s => s.Pages))
        {
            var dto = NotebookJson.ToDto(page);
            dto.Modified = default;
            sb.AppendLine($"page {Ref(page)} {page.LoadError} elements {string.Join(",", page.Elements.Select(Ref))}");
            sb.AppendLine(JsonSerializer.Serialize(dto, NotebookJsonContext.Default.PageDto));
        }

        foreach (var id in notebook.Sections.SelectMany(s => s.Pages).SelectMany(p => p.Elements).OfType<ImageElement>().Select(i => i.MediaId).Distinct().Order())
            sb.AppendLine($"media {id} {Convert.ToBase64String(notebook.GetMedia(id) ?? [])}");
        return sb.ToString();
    }

    private static int Ref(object o) => RuntimeHelpers.GetHashCode(o);

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyRevertRedo_RestoresModel(string name)
    {
        var notebook = AllKinds();
        var before = Snapshot(notebook);

        notebook.Execute(Cases[name](notebook));
        var applied = Snapshot(notebook);
        Assert.NotEqual(before, applied);
        Assert.True(notebook.Undo.CanUndo);
        notebook.Undo.Undo();
        Assert.Equal(before, Snapshot(notebook));
        notebook.Undo.Redo();
        Assert.Equal(applied, Snapshot(notebook));
        Assert.Equal(WithoutReferences(applied), WithoutReferences(Snapshot(Reload(notebook))));
    }

    private static string WithoutReferences(string snapshot) =>
        string.Join("\n", snapshot.Split('\n').Where(l => !l.StartsWith("page ", StringComparison.Ordinal) && !l.StartsWith("sections ", StringComparison.Ordinal)));

    [Fact]
    public void DeleteLastPage_LeavesEmptyPage()
    {
        var notebook = AllKinds();
        var section = notebook.Sections[1];

        notebook.Execute(new DeletePagesCommand(section.Pages.ToList()));

        var page = Assert.Single(section.Pages);
        Assert.Empty(page.Elements);
        Assert.DoesNotContain(page.Id, new[] { "p2", "p3" });
        notebook.Undo.Undo();
        Assert.Equal(["p2", "p3"], section.Pages.Select(p => p.Id));
    }

    [Fact]
    public void DeleteLastSection_Throws()
    {
        var notebook = Notebook.Create();

        Assert.Throws<InvalidOperationException>(() => notebook.Execute(new DeleteSectionCommand(notebook.Sections[0])));
        Assert.Single(notebook.Sections);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public void MovePages_AcrossSections_KeepsOrder()
    {
        var notebook = AllKinds();
        var (first, second) = (notebook.Sections[0], notebook.Sections[1]);
        var page = First(notebook);

        notebook.Execute(new MovePagesCommand([second.Pages[1], page], second, 1));

        Assert.Equal(["p2", page.Id, "p3"], second.Pages.Select(p => p.Id));
        var fresh = Assert.Single(first.Pages);
        Assert.NotSame(page, fresh);
        notebook.Undo.Undo();
        Assert.Same(page, Assert.Single(first.Pages));
        Assert.Equal(["p2", "p3"], second.Pages.Select(p => p.Id));
    }

    [Fact]
    public void MovePages_ToEndOfOwnSection_MovesBlock()
    {
        var notebook = AllKinds();
        var section = notebook.Sections[1];
        notebook.Execute(new AddPageCommand(section, 2, "p4"));

        notebook.Execute(new MovePagesCommand([section.Pages[0]], section, 3));

        Assert.Equal(["Three", "p4", "Two"], section.Pages.Select(p => p.Title));
    }

    [Fact]
    public void SetText_Empty_DeletesElementInSameStep()
    {
        var notebook = AllKinds();
        var page = First(notebook);

        notebook.Execute(new SetTextCommand(page, "text", [Paragraph(""), new NoteParagraph { List = NoteListKind.Todo }]));

        Assert.DoesNotContain(page.Elements, e => e.Id == "text");
        notebook.Undo.Undo();
        Assert.Equal("text", page.Elements[0].Id);
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public void SetText_UnsafeLinkOrColor_Throws()
    {
        var notebook = AllKinds();
        var page = First(notebook);

        Assert.Throws<ArgumentException>(() => notebook.Execute(new SetTextCommand(page, "text",
            [new NoteParagraph { Runs = [new NoteRun("x", new NoteRunFormat(Link: "javascript:alert(1)"))] }])));
        Assert.Throws<ArgumentException>(() => notebook.Execute(new SetTextCommand(page, "text",
            [new NoteParagraph { Runs = [new NoteRun("x", new NoteRunFormat(Color: "red;background:url(x)"))] }])));
        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public void Resize_Ink_ScalesPoints()
    {
        var notebook = AllKinds();
        var page = First(notebook);

        notebook.Execute(new ResizeElementsCommand(page, new Dictionary<string, NoteBounds> { ["ink"] = new(10, 20, 60, 20) }));

        var ink = (InkElement)page.Elements.Single(e => e.Id == "ink");
        Assert.Equal(new NoteBounds(10, 20, 60, 20), ink.Bounds);
        Assert.Equal([new InkPoint(0, 0, 0.5), new InkPoint(60, 20, 1)], ink.Points);
    }

    [Fact]
    public void Resize_Table_ScalesColumns()
    {
        var notebook = AllKinds();
        var page = First(notebook);

        notebook.Execute(new ResizeElementsCommand(page, new Dictionary<string, NoteBounds> { ["table"] = new(200, 300, 440, 0) }));

        Assert.Equal([200d, 240d], ((TableElement)page.Elements.Single(e => e.Id == "table")).ColumnWidths);
    }

    [Fact]
    public void AddStroke_SinglePoint_IsValid()
    {
        var notebook = Notebook.Create();
        var page = First(notebook);
        var command = new AddStrokeCommand(page, [new InkPoint(50, 60, 0.5)], "#000000", 4, InkTool.Pen);

        notebook.Execute(command);

        var dot = Assert.IsType<InkElement>(Assert.Single(page.Elements));
        Assert.Same(command.Stroke, dot);
        Assert.Equal(new NoteBounds(48, 58, 4, 4), dot.Bounds);
        Assert.Equal([new InkPoint(2, 2, 0.5)], dot.Points);
        var reloaded = (InkElement)Reload(notebook).Sections[0].Pages[0].Elements.Single();
        Assert.Equal(dot.Points, reloaded.Points);
    }

    [Fact]
    public void AddStroke_HullCoversPointsAndWidth()
    {
        var notebook = Notebook.Create();
        var page = First(notebook);

        notebook.Execute(new AddStrokeCommand(page, [new InkPoint(10, 40, 0.2), new InkPoint(30, 20, 2)], "#00FF00", 2, InkTool.Highlighter));

        var stroke = (InkElement)page.Elements.Single();
        Assert.Equal(new NoteBounds(9, 19, 22, 22), stroke.Bounds);
        Assert.Equal([new InkPoint(1, 21, 0.2), new InkPoint(21, 1, 1)], stroke.Points);
        Assert.Equal(InkTool.Highlighter, stroke.Tool);
        Assert.Throws<ArgumentException>(() => notebook.Execute(new AddStrokeCommand(page, [], "#000000", 2, InkTool.Pen)));
    }

    [Fact]
    public void AddImage_LimitsWidthKeepsAspect()
    {
        var notebook = Notebook.Create();
        var page = First(notebook);
        var data = PngOfSize(1200, 600);
        var command = new AddImageCommand(page, data, "image/png", 5, 6, 600);

        notebook.Execute(command);

        Assert.Equal(new NoteBounds(5, 6, 600, 300), command.Image.Bounds);
        Assert.Equal(data, notebook.GetMedia(command.Image.MediaId));
        Assert.Equal("image/png", command.Image.ContentType);
        Assert.Throws<ArgumentException>(() => notebook.Execute(new AddImageCommand(page, data, "text/html", 0, 0, 600)));
    }

    [Fact]
    public void Table_DeletingEveryRowOrColumn_Throws()
    {
        var notebook = AllKinds();
        var page = First(notebook);

        Assert.Throws<ArgumentException>(() => notebook.Execute(new DeleteTableRowsCommand(page, "table", 0, 2)));
        Assert.Throws<ArgumentException>(() => notebook.Execute(new DeleteTableColumnsCommand(page, "table", 0, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => notebook.Execute(new InsertTableRowsCommand(page, "table", 3, 1)));
    }

    [Fact]
    public void Table_InsertColumn_AddsEmptyCellsAndWidth()
    {
        var notebook = AllKinds();
        var page = First(notebook);

        notebook.Execute(new InsertTableColumnsCommand(page, "table", 1, 1));

        var table = (TableElement)page.Elements.Single(e => e.Id == "table");
        Assert.All(table.Rows, r => Assert.Equal(3, r.Count));
        Assert.Equal(table.ColumnWidths.Sum(), table.Width);
        Assert.Equal("c", table.Rows[1][0].Paragraphs[0].Runs[0].Text);
        Assert.Equal("d", table.Rows[1][2].Paragraphs[0].Runs[0].Text);
    }

    [Fact]
    public void NoOpCommands_RecordNoUndoStep()
    {
        var notebook = AllKinds();
        var page = First(notebook);
        var ink = page.Elements.Single(e => e.Id == "ink");
        IDocumentCommand<Notebook>[] commands =
        [
            new RenameSectionCommand(notebook.Sections[1], "Second"),
            new SetSectionColorCommand(notebook.Sections[1], "#00AA00"),
            new MoveSectionCommand(notebook.Sections[0], 1),
            new MoveSectionCommand(notebook.Sections[0], 0),
            new RenamePageCommand(page, "Kinds"),
            new SetPageLevelCommand([page], 1),
            new MovePagesCommand([notebook.Sections[1].Pages[0]], notebook.Sections[1], 1),
            new DeleteElementsCommand(page, []),
            new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["ink"] = (ink.X, ink.Y) }),
            new ResizeElementsCommand(page, new Dictionary<string, NoteBounds> { ["ink"] = ink.Bounds }),
            new ReorderElementsCommand(page, ["table"], true),
            new ReorderElementsCommand(page, ["text"], false),
            new SetTextCommand(page, "text", ((TextElement)page.Elements[0]).Paragraphs.ToList()),
            new SetShapeStyleCommand(page, ["shape"], "#00FF00", 2, "#CCCCCC"),
            new SetShapeStyleCommand(page, ["text"], "#00FF00", null, null),
            new SetTableCellCommand(page, "table", 0, 0, [Paragraph("a")]),
            new SetTableColumnWidthsCommand(page, "table", [100, 120]),
            new SetTableHeaderRowCommand(page, "table", true),
            new EraseStrokesCommand(page, [])
        ];

        foreach (var command in commands) notebook.Execute(command);

        Assert.False(notebook.Undo.CanUndo);
        Assert.False(notebook.Modified);
        Assert.Equal(0, page.Version);
    }

    [Fact]
    public void Changed_RaisedOncePerTransaction_AndAfterUndo()
    {
        var notebook = AllKinds();
        var page = First(notebook);
        var events = new List<NotebookChangedEventArgs>();
        notebook.Changed += (_, e) => events.Add(e);

        using (notebook.Undo.BeginTransaction("Two"))
        {
            notebook.Execute(new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["ink"] = (0, 0) }));
            notebook.Execute(new RenamePageCommand(page, "Renamed"));
            Assert.Empty(events);
        }

        var single = Assert.Single(events);
        Assert.Equal(NotebookChangeKind.Content | NotebookChangeKind.Pages, single.Kind);
        Assert.Equal([page], single.Pages);
        Assert.True(notebook.Modified && page.Modified);

        notebook.Undo.Undo();
        Assert.Equal(2, events.Count);
        Assert.Equal("Kinds", page.Title);
        notebook.Undo.Redo();
        Assert.Equal(3, events.Count);
        notebook.Execute(new RenameSectionCommand(notebook.Sections[0], "S"));
        Assert.Equal(NotebookChangeKind.Sections, events[^1].Kind);
    }

    [Fact]
    public void ElementVersion_ChangesOnlyForTouchedElements()
    {
        var notebook = AllKinds();
        var page = First(notebook);
        var shape = page.Elements.Single(e => e.Id == "shape");

        notebook.Execute(new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["ink"] = (7, 8) }));

        var moved = page.Elements.Single(e => e.Id == "ink");
        Assert.Equal(page.Version, moved.Version);
        Assert.NotEqual(0, moved.Version);
        Assert.Equal(0, shape.Version);
        notebook.Undo.Undo();
        var restored = page.Elements.Single(e => e.Id == "ink");
        Assert.Equal(page.Version, restored.Version);
        Assert.Equal(0, shape.Version);
    }

    [Fact]
    public void Commands_OnErrorPage_AreRefused()
    {
        var notebook = new NotebookBuilder().Page("ok").RawPage("bad", "{ broken").Load();
        var bad = Page(notebook, "bad");
        IDocumentCommand<Notebook>[] refused =
        [
            new RenamePageCommand(bad, "x"),
            new SetPageLevelCommand([bad], 1),
            new DuplicatePagesCommand([bad]),
            new AddElementCommand(bad, new ShapeElement()),
            new AddStrokeCommand(bad, [new InkPoint(1, 1, 0.5)], "#000000", 2, InkTool.Pen),
            new AddTableCommand(bad, 1, 1, 0, 0)
        ];

        foreach (var command in refused) Assert.Throws<InvalidOperationException>(() => notebook.Execute(command));
        Assert.False(notebook.Undo.CanUndo);

        notebook.Execute(new MovePagesCommand([bad], notebook.Sections[0], 0));
        notebook.Execute(new DeletePagesCommand([bad]));
        Assert.Equal(["ok"], notebook.Sections[0].Pages.Select(p => p.Id));
    }

    [Fact]
    public void Commands_PageOfOtherNotebook_Throws()
    {
        var notebook = Notebook.Create();
        var foreign = First(Notebook.Create());

        Assert.Throws<ArgumentException>(() => notebook.Execute(new RenamePageCommand(foreign, "x")));
        Assert.Throws<ArgumentException>(() => notebook.Execute(new AddTableCommand(foreign, 1, 1, 0, 0)));
    }

    [Fact]
    public void UnknownElement_IsOpaque_MovableAndSavedWithRawJson()
    {
        var notebook = new NotebookBuilder()
            .Page("p1", "{\"type\":\"hologram\",\"id\":\"h\",\"x\":3,\"y\":4,\"width\":5,\"height\":6,\"beam\":{\"on\":true}}," +
                        "{\"type\":\"shape\",\"id\":\"s\",\"kind\":\"star\",\"width\":10}," + Text("t", "{\"text\":\"kept\"}"))
            .Load();
        var page = Page(notebook, "p1");

        Assert.Null(page.LoadError);
        var hologram = Assert.IsType<OpaqueElement>(page.Elements[0]);
        Assert.Equal(new NoteBounds(3, 4, 5, 6), hologram.Bounds);
        Assert.IsType<OpaqueElement>(page.Elements[1]);
        Assert.IsType<TextElement>(page.Elements[2]);

        notebook.Execute(new ResizeElementsCommand(page, new Dictionary<string, NoteBounds> { ["h"] = new(0, 0, 50, 50) }));
        Assert.False(notebook.Undo.CanUndo);
        notebook.Execute(new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["h"] = (10, 20) }));

        var json = JsonDocument.Parse(Unzip(Save(notebook))["pages/p1.json"]).RootElement.GetProperty("elements");
        Assert.Equal("{\"type\":\"hologram\",\"id\":\"h\",\"x\":10,\"y\":20,\"width\":5,\"height\":6,\"beam\":{\"on\":true}}", json[0].GetRawText());
        Assert.Equal("star", json[1].GetProperty("kind").GetString());
        var reloaded = Page(Reload(notebook), "p1");
        Assert.Equal(new NoteBounds(10, 20, 5, 6), reloaded.Elements[0].Bounds);
        Assert.Equal("kept", ((TextElement)reloaded.Elements[2]).Paragraphs[0].Runs[0].Text);
    }

    [Fact]
    public void Commands_NonFiniteValues_AreRefused()
    {
        var notebook = AllKinds();
        var page = First(notebook);
        var nanSize = new NoteParagraph { Runs = [new NoteRun("x", new NoteRunFormat(SizePt: double.NaN))] };
        Func<IDocumentCommand<Notebook>>[] commands =
        [
            () => new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["ink"] = (10, double.NaN) }),
            () => new MoveElementsCommand(page, new Dictionary<string, (double X, double Y)> { ["ink"] = (double.PositiveInfinity, 2.25) }),
            () => new ResizeElementsCommand(page, new Dictionary<string, NoteBounds> { ["shape"] = new(1, 1, double.NaN, 10) }),
            () => new ResizeElementsCommand(page, new Dictionary<string, NoteBounds> { ["table"] = new(1, 1, 0, 0) }),
            () => new SetTextCommand(page, "text", [nanSize]),
            () => new SetTableCellCommand(page, "table", 0, 0, [nanSize]),
            () => new AddStrokeCommand(page, [new InkPoint(1, 1, double.NaN)], "#000000", 2, InkTool.Pen),
            () => new AddStrokeCommand(page, [new InkPoint(1, double.NegativeInfinity, 0.5)], "#000000", 2, InkTool.Pen),
            () => new AddElementCommand(page, new InkElement { Id = "i2", Points = [new InkPoint(double.NaN, 0, 0.5)] }),
            () => new AddElementCommand(page, new TableElement { Id = "t2", ColumnWidths = [double.NaN], Rows = [[new NoteTableCell()]] }),
            () => new AddElementCommand(page, new TextElement
            {
                Id = "x2", Paragraphs = [new NoteParagraph { Runs = [new NoteRun("x", new NoteRunFormat(SizePt: double.PositiveInfinity))] }]
            }),
            () => new AddElementCommand(page, new ShapeElement { Id = "s2", StrokeWidth = double.NaN }),
            () => new SetShapeStyleCommand(page, ["ink"], null, 0, null),
            () => new SetShapeStyleCommand(page, ["shape"], null, double.NaN, null),
            () => new SetTableColumnWidthsCommand(page, "table", [double.NaN, 5]),
            () => new DuplicateElementsCommand(page, ["shape"], double.NaN),
            () => new AddImageCommand(page, Png, "image/png", 0, double.NaN, 600),
            () => new AddTableCommand(page, 1, 1, double.NaN, 0)
        ];

        foreach (var command in commands) Assert.ThrowsAny<ArgumentException>(() => notebook.Execute(command()));

        Assert.False(notebook.Undo.CanUndo);
        Assert.NotEmpty(Save(notebook));
    }

    [Fact]
    public void DuplicatePages_CopiesStartWithFreshVersions_AndKeysNeverRepeat()
    {
        var notebook = AllKinds();
        var source = First(notebook);
        for (var i = 1; i <= 3; i++) notebook.Execute(new MoveElementsCommand(source, new Dictionary<string, (double X, double Y)> { ["ink"] = (i, i) }));
        Assert.Equal(3, source.Elements.Single(e => e.Id == "ink").Version);
        var duplicate = new DuplicatePagesCommand([source]);
        notebook.Execute(duplicate);
        var copy = Assert.Single(duplicate.Copies);

        Assert.All(copy.Elements, e => Assert.True(e.Version <= copy.Version));
        var seen = copy.Elements.ToDictionary(e => e.Id, e => e.Version);
        foreach (var id in new[] { "shape", "text", "ink" })
        {
            notebook.Execute(new MoveElementsCommand(copy, new Dictionary<string, (double X, double Y)> { [id] = (500, 500) }));
            var version = copy.Elements.Single(e => e.Id == id).Version;
            Assert.True(version > seen[id], $"{id}: {version} <= {seen[id]}");
            seen[id] = version;
        }
    }

    [Fact]
    public void SetShapeStyle_InkWidth_UpdatesHull()
    {
        var notebook = Notebook.Create();
        var page = First(notebook);
        var add = new AddStrokeCommand(page, [new InkPoint(10, 10, 0.5), new InkPoint(30, 20, 0.5)], "#000000", 2, InkTool.Pen);
        notebook.Execute(add);
        Assert.Equal(new NoteBounds(9, 9, 22, 12), add.Stroke.Bounds);

        notebook.Execute(new SetShapeStyleCommand(page, [add.Stroke.Id], null, 12, null));

        var wide = (InkElement)page.Elements.Single();
        Assert.Equal(new NoteBounds(4, 4, 32, 22), wide.Bounds);
        Assert.Equal([new InkPoint(6, 6, 0.5), new InkPoint(26, 16, 0.5)], wide.Points);
        notebook.Undo.Undo();
        var thin = (InkElement)page.Elements.Single();
        Assert.Equal(new NoteBounds(9, 9, 22, 12), thin.Bounds);
        Assert.Equal([new InkPoint(1, 1, 0.5), new InkPoint(21, 11, 0.5)], thin.Points);
    }

    [Fact]
    public void EraseStrokes_TwoStrokes_OneUndoStep()
    {
        var notebook = Notebook.Create();
        var page = First(notebook);
        var first = new AddStrokeCommand(page, [new InkPoint(1, 1, 0.5)], "#000000", 2, InkTool.Pen);
        var second = new AddStrokeCommand(page, [new InkPoint(9, 9, 0.5)], "#000000", 2, InkTool.Pen);
        notebook.Execute(first);
        notebook.Execute(second);

        notebook.Execute(new EraseStrokesCommand(page, [first.Stroke.Id, second.Stroke.Id]));

        Assert.Empty(page.Elements);
        notebook.Undo.Undo();
        Assert.Equal(2, page.Elements.Count);
    }

    [Fact]
    public void DuplicateElements_Opaque_GetsNewIdInSavedJson()
    {
        const string raw = "{\"type\":\"hologram\",\"id\":\"h\",\"x\":3,\"y\":4,\"beam\":1}";
        var notebook = new NotebookBuilder().Page("p1", raw).Load();
        var duplicate = new DuplicateElementsCommand(Page(notebook, "p1"), ["h"], 10);

        notebook.Execute(duplicate);

        var json = JsonDocument.Parse(Unzip(Save(notebook))["pages/p1.json"]).RootElement.GetProperty("elements");
        Assert.Equal(raw, json[0].GetRawText());
        Assert.Equal(duplicate.Copies[0].Id, json[1].GetProperty("id").GetString());
        Assert.Equal(13, json[1].GetProperty("x").GetDouble());
        Assert.Equal(1, json[1].GetProperty("beam").GetInt32());
    }

    [Fact]
    public void DeletePages_OnlyBlankPage_IsNoOp()
    {
        var notebook = Notebook.Create();

        notebook.Execute(new DeletePagesCommand([First(notebook)]));

        Assert.False(notebook.Undo.CanUndo);
    }

    [Fact]
    public void AddImage_CopiesTheBytes()
    {
        var notebook = Notebook.Create();
        var data = (byte[])Png.Clone();
        var command = new AddImageCommand(First(notebook), data, "image/png", 0, 0, 600);
        notebook.Execute(command);

        data[^1] = 99;

        Assert.Equal(Png, notebook.GetMedia(command.Image.MediaId));
    }

    [Fact]
    public void Commands_NullIds_ThrowArgumentNull()
    {
        var notebook = AllKinds();
        var page = First(notebook);

        Assert.Throws<ArgumentNullException>(() => notebook.Execute(new DeleteElementsCommand(page, null!)));
        Assert.Throws<ArgumentNullException>(() => notebook.Execute(new MoveElementsCommand(page, null!)));
        Assert.Throws<ArgumentNullException>(() => notebook.Execute(new ResizeElementsCommand(page, null!)));
        Assert.Throws<ArgumentNullException>(() => notebook.Execute(new ReorderElementsCommand(page, null!, true)));
        Assert.Throws<ArgumentNullException>(() => notebook.Execute(new DuplicateElementsCommand(page, null!, 1)));
        Assert.Throws<ArgumentNullException>(() => notebook.Execute(new SetShapeStyleCommand(page, null!, null, null, null)));
        Assert.Throws<ArgumentNullException>(() => notebook.Execute(new EraseStrokesCommand(page, null!)));
    }
}
