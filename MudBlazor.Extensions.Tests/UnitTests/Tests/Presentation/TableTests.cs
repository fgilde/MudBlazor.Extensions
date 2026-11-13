using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class TableTests
{
    internal const string StyleId = "{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}";
    internal const string TableUri = "http://schemas.openxmlformats.org/drawingml/2006/table";
    internal const string Borders = "<a:lnL w=\"12700\"><a:solidFill><a:srgbClr val=\"00FF00\"/></a:solidFill></a:lnL><a:lnB w=\"25400\"><a:noFill/></a:lnB>";

    internal static readonly string MergedTable = Table(3, $"<a:tblPr firstRow=\"1\" bandRow=\"1\"><a:tableStyleId>{StyleId}</a:tableStyleId></a:tblPr>", [1000000, 2000000, 3000000],
        Tc("A", " rowSpan=\"3\"") + Tc("B") + Tc("C"),
        Tc("", " vMerge=\"1\"") + Tc("D", " gridSpan=\"2\"") + Tc("", " hMerge=\"1\""),
        Tc("", " vMerge=\"1\"") + Tc("E") + Tc("F"),
        Tc("G") + Tc("H") + Tc("I", "", $"<a:tcPr marL=\"1\" marR=\"2\" marT=\"3\" marB=\"4\" anchor=\"ctr\">{Borders}<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill></a:tcPr>"));

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["InsertTable"] = p => new InsertTableCommand(p.Slides[0], 2, 2, new ShapeTransform(0, 0, 1000, 1000)),
        ["SetCellText"] = p => new SetTableCellTextCommand(p.Slides[0], 3, 3, 0, Body("X")),
        ["InsertRows"] = p => new InsertTableRowsCommand(p.Slides[0], 3, 1, 2),
        ["DeleteRows"] = p => new DeleteTableRowsCommand(p.Slides[0], 3, 0, 2),
        ["DeleteAllRows"] = p => new DeleteTableRowsCommand(p.Slides[0], 3, 0, 4),
        ["InsertColumns"] = p => new InsertTableColumnsCommand(p.Slides[0], 3, 2, 1),
        ["DeleteColumns"] = p => new DeleteTableColumnsCommand(p.Slides[0], 3, 1, 1),
        ["DeleteAllColumns"] = p => new DeleteTableColumnsCommand(p.Slides[0], 3, 0, 3),
        ["Merge"] = p => new MergeTableCellsCommand(p.Slides[0], 3, 2, 1, 2, 2),
        ["Split"] = p => new SplitTableCellCommand(p.Slides[0], 3, 0, 0),
        ["SetSizes"] = p => new SetTableSizesCommand(p.Slides[0], 3, [1, 2, 3], [4, 5, 6, 7]),
        ["SetCellFill"] = p => new SetTableCellFillCommand(p.Slides[0], 3, [(0, 1), (3, 2)], new ShapeFill(ShapeFillKind.Solid, "#00FF00")),
        ["ClearCellFill"] = p => new SetTableCellFillCommand(p.Slides[0], 3, [(3, 2)], null),
        ["SetOptions"] = p => new SetTableOptionsCommand(p.Slides[0], 3, false, null, true, true, null, true),
        ["SetTransforms"] = p => new SetTransformsCommand(p.Slides[0], new Dictionary<uint, ShapeTransform> { [3] = new(0, 0, 3000000, 800000) })
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    internal static string Tc(string text, string attributes = "", string tcPr = "<a:tcPr/>") =>
        $"<a:tc{attributes}><a:txBody><a:bodyPr/><a:lstStyle/>{(text.Length == 0 ? "<a:p><a:endParaRPr lang=\"en-US\"/></a:p>" : $"<a:p><a:r><a:rPr lang=\"en-US\"/><a:t>{text}</a:t></a:r></a:p>")}</a:txBody>{tcPr}</a:tc>";

    internal static string Table(uint id, string tblPr, long[] widths, params string[] rows) =>
        $"<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"{id}\" name=\"Table {id - 1}\"/><p:cNvGraphicFramePr><a:graphicFrameLocks noGrp=\"1\"/></p:cNvGraphicFramePr><p:nvPr/></p:nvGraphicFramePr>" +
        $"<p:xfrm><a:off x=\"1000000\" y=\"1000000\"/><a:ext cx=\"{widths.Sum()}\" cy=\"{rows.Length * 400000}\"/></p:xfrm>" +
        $"<a:graphic><a:graphicData uri=\"{TableUri}\"><a:tbl>{tblPr}<a:tblGrid>{string.Concat(widths.Select(w => $"<a:gridCol w=\"{w}\"/>"))}</a:tblGrid>" +
        string.Concat(rows.Select(r => $"<a:tr h=\"400000\">{r}</a:tr>")) + "</a:tbl></a:graphicData></a:graphic></p:graphicFrame>";

    internal static Presentation Deck() => new PptxBuilder()
        .Slide(Shape(2, "Text", Xfrm(0, 0, 100, 100), "<a:p><a:r><a:t>apple</a:t></a:r></a:p>") + MergedTable)
        .Slide(Shape(2, "Box", Xfrm(0, 0, 10, 10)))
        .Load();

    internal static TextBody Body(params string[] paragraphs) => new() { Paragraphs = paragraphs.Select(p => new TextParagraph { Runs = [new TextRun(p)] }).ToList() };

    internal static TableShape Table(Presentation presentation, uint id = 3, int slide = 0) =>
        (TableShape)SlideShape.Flatten(presentation.Slides[slide].Shapes).Single(s => s.Id == id);

    internal static string Text(TableCell cell) => string.Join("/", cell.Text?.Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))) ?? []);

    internal static string Grid(TableShape table) => string.Join(" | ", table.Rows.Select(row => string.Join(" ", row.Cells.Select(cell =>
        cell.IsMergedAway ? "_" : (Text(cell) is { Length: > 0 } text ? text : "·") + (cell.RowSpan * cell.ColumnSpan > 1 ? $"[{cell.RowSpan}x{cell.ColumnSpan}]" : "")))));

    [Fact]
    public void Reader_Table_ReadsGridRowsCellsAndMerges()
    {
        var presentation = Deck();
        var table = Table(presentation);

        Assert.Equal(("Table 2", new ShapeTransform(1000000, 1000000, 6000000, 1600000)), (table.Name, table.Transform!.Value));
        Assert.Equal([1000000L, 2000000L, 3000000L], table.ColumnWidths);
        Assert.Equal([400000L, 400000L, 400000L, 400000L], table.Rows.Select(r => r.Height));
        Assert.All(table.Rows, r => Assert.Equal(3, r.Cells.Count));
        Assert.Equal("A[3x1] B C | _ D[1x2] _ | _ E F | G H I", Grid(table));
        Assert.Equal(StyleId, table.StyleId);
        Assert.Equal((true, false, false, false, true, false),
            (table.FirstRow, table.FirstColumn, table.LastRow, table.LastColumn, table.BandedRows, table.BandedColumns));
        var cell = table.Rows[3].Cells[2];
        Assert.Equal(new ShapeFill(ShapeFillKind.Solid, "#FF0000"), cell.Fill);
        Assert.Equal((1L, 3L, 2L, 4L, TextAnchor.Middle), (cell.MarginLeft!.Value, cell.MarginTop!.Value, cell.MarginRight!.Value, cell.MarginBottom!.Value, cell.Anchor!.Value));
        var plain = table.Rows[0].Cells[1];
        Assert.Null(plain.Fill);
        Assert.Null(plain.MarginLeft);
        Assert.Null(plain.Anchor);
        Assert.False(presentation.HasOpaqueContent);
    }

    [Fact]
    public void InsertTable_DefaultStyleAndSizes()
    {
        var presentation = Deck();
        var slide = presentation.Slides[1];
        var command = new InsertTableCommand(slide, 2, 3, new ShapeTransform(100, 200, 3000002, 999));

        presentation.Execute(command);

        var table = command.Table;
        Assert.Same(table, slide.Shapes[^1]);
        Assert.Equal((3u, "Table 2"), (table.Id, table.Name));
        Assert.Equal(TableShape.DefaultStyleId, table.StyleId);
        Assert.Equal((true, false, false, false, true, false),
            (table.FirstRow, table.FirstColumn, table.LastRow, table.LastColumn, table.BandedRows, table.BandedColumns));
        Assert.Equal([1000000L, 1000000L, 1000000L], table.ColumnWidths);
        Assert.Equal([370840L, 370840L], table.Rows.Select(r => r.Height));
        Assert.Equal(new ShapeTransform(100, 200, 3000000, 741680), table.Transform);
        Assert.Equal("· · · | · · ·", Grid(table));
        Assert.All(table.Rows.SelectMany(r => r.Cells), c => Assert.Null(c.Fill));
        Assert.Equal("Insert table", presentation.Undo.UndoDescription);
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new InsertTableCommand(slide, 0, 3, default)));
    }

    [Fact]
    public void InsertRow_ThroughVerticalMerge_GrowsRowSpan()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var table = Table(presentation);

        presentation.Execute(new InsertTableRowsCommand(slide, 3, 1, 1));

        Assert.Equal("A[4x1] B C | _ · · | _ D[1x2] _ | _ E F | G H I", Grid(table));
        Assert.Equal(400000, table.Rows[1].Height);
        Assert.Equal(2000000, table.Transform!.Value.Height);
        Assert.Equal("Insert rows", presentation.Undo.UndoDescription);

        presentation.Execute(new InsertTableRowsCommand(slide, 3, 0, 1));
        Assert.Equal("· · · | A[4x1] B C | _ · · | _ D[1x2] _ | _ E F | G H I", Grid(table));
        presentation.Execute(new InsertTableRowsCommand(slide, 3, 5, 1));
        Assert.Equal("· · · | A[4x1] B C | _ · · | _ D[1x2] _ | _ E F | · · · | G H I", Grid(table));
        Assert.All(table.Rows, r => Assert.Equal(3, r.Cells.Count));
    }

    [Fact]
    public void DeleteColumn_ThroughHorizontalMerge_ShrinksGridSpan()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var table = Table(presentation);
        var anchor = table.Rows[1].Cells[1];

        presentation.Execute(new DeleteTableColumnsCommand(slide, 3, 2, 1));
        Assert.Equal("A[3x1] B | _ D | _ E | G H", Grid(table));
        Assert.Equal([1000000L, 2000000L], table.ColumnWidths);
        Assert.Equal(3000000, table.Transform!.Value.Width);
        Assert.Equal("Delete columns", presentation.Undo.UndoDescription);

        presentation.Undo.Undo();
        presentation.Execute(new DeleteTableColumnsCommand(slide, 3, 1, 1));
        Assert.Equal("A[3x1] C | _ D | _ F | G I", Grid(table));
        Assert.Same(anchor.Source, table.Rows[1].Cells[1].Source);

        presentation.Undo.Undo();
        presentation.Execute(new InsertTableColumnsCommand(slide, 3, 2, 2));
        Assert.Equal("A[3x1] B · · C | _ D[1x4] _ _ _ | _ E · · F | G H · · I", Grid(table));
        Assert.Equal([1000000L, 2000000L, 2000000L, 2000000L, 3000000L], table.ColumnWidths);
    }

    [Fact]
    public void DeleteRow_WithMergeAnchor_MovesAnchorDown()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var table = Table(presentation);
        var anchor = table.Rows[0].Cells[0];

        presentation.Execute(new DeleteTableRowsCommand(slide, 3, 0, 1));

        Assert.Equal("A[2x1] D[1x2] _ | _ E F | G H I", Grid(table));
        Assert.Same(anchor.Source, table.Rows[0].Cells[0].Source);
        Assert.Equal(1200000, table.Transform!.Value.Height);

        presentation.Execute(new DeleteTableRowsCommand(slide, 3, 0, 2));
        Assert.Equal("G H I", Grid(table));
        presentation.Execute(new DeleteTableRowsCommand(slide, 3, 0, 1));
        Assert.DoesNotContain(table, slide.Shapes);
        Assert.Equal("Delete rows", presentation.Undo.UndoDescription);
        presentation.Undo.Undo();
        Assert.Contains(table, slide.Shapes);
    }

    [Fact]
    public void Merge_CollectsTexts()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var table = Table(presentation);

        presentation.Execute(new MergeTableCellsCommand(slide, 3, 2, 1, 2, 2));

        Assert.Equal("A[3x1] B C | _ D[1x2] _ | _ E/F/H/I[2x2] _ | G _ _", Grid(table));
        Assert.Equal("Merge cells", presentation.Undo.UndoDescription);
        Assert.Throws<ArgumentException>(() => presentation.Execute(new MergeTableCellsCommand(slide, 3, 0, 2, 2, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new MergeTableCellsCommand(slide, 3, 3, 0, 2, 1)));

        presentation.Execute(new SetTableCellTextCommand(slide, 3, 0, 1, Body("")));
        presentation.Execute(new MergeTableCellsCommand(slide, 3, 0, 1, 1, 2));
        Assert.Equal("A[3x1] C[1x2] _ | _ D[1x2] _ | _ E/F/H/I[2x2] _ | G _ _", Grid(table));
    }

    [Fact]
    public void Split_RestoresCells()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var table = Table(presentation);

        presentation.Execute(new SplitTableCellCommand(slide, 3, 1, 1));
        Assert.Equal("A[3x1] B C | _ D · | _ E F | G H I", Grid(table));
        presentation.Execute(new SplitTableCellCommand(slide, 3, 0, 0));
        Assert.Equal("A B C | · D · | · E F | G H I", Grid(table));
        Assert.Equal("Split cells", presentation.Undo.UndoDescription);

        presentation.Execute(new MergeTableCellsCommand(slide, 3, 2, 1, 2, 2));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SplitTableCellCommand(slide, 3, 2, 2)));
        presentation.Execute(new SplitTableCellCommand(slide, 3, 2, 1));
        Assert.Equal("A B C | · D · | · E/F/H/I · | G · ·", Grid(table));
        Assert.All(table.Rows.SelectMany(r => r.Cells), c => Assert.Equal((1, 1, false), (c.RowSpan, c.ColumnSpan, c.IsMergedAway)));
    }

    [Fact]
    public void SetTransforms_Table_ScalesColumnsAndRows()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var table = Table(presentation);
        var rows = table.Rows;

        presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [3] = new(5, 6, 6000000, 1600000) }));
        Assert.Same(rows, table.Rows);

        presentation.Execute(new SetTransformsCommand(slide, new Dictionary<uint, ShapeTransform> { [3] = new(5, 6, 3000001, 800000) }));
        Assert.Equal([500000L, 1000000L, 1500001L], table.ColumnWidths);
        Assert.Equal([200000L, 200000L, 200000L, 200000L], table.Rows.Select(r => r.Height));
        Assert.Equal("A[3x1] B C | _ D[1x2] _ | _ E F | G H I", Grid(table));

        presentation.Undo.Undo();
        Assert.Equal([1000000L, 2000000L, 3000000L], table.ColumnWidths);
        Assert.Same(rows, table.Rows);
    }

    [Fact]
    public void SetTableSizes_SetsFrameToSums()
    {
        var presentation = Deck();
        var table = Table(presentation);

        presentation.Execute(new SetTableSizesCommand(presentation.Slides[0], 3, [10, 20, 30], [1, 2, 3, 4]));

        Assert.Equal(new ShapeTransform(1000000, 1000000, 60, 10), table.Transform);
        Assert.Equal([1L, 2L, 3L, 4L], table.Rows.Select(r => r.Height));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTableSizesCommand(presentation.Slides[0], 3, [10, 20], [1, 2, 3, 4])));
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = Deck();
        var before = SlideCommandTests.Snapshot(presentation);
        var command = Cases[name](presentation);

        command.Apply(presentation);
        var applied = SlideCommandTests.Snapshot(presentation);
        Assert.NotEqual(before, applied);
        command.Revert(presentation);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
        command.Apply(presentation);
        Assert.Equal(applied, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void NoOpTableCommands_RecordNoUndoStep()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var table = Table(presentation);
        var before = SlideCommandTests.Snapshot(presentation);

        foreach (var command in new IDocumentCommand<Presentation>[]
                 {
                     new SetTableCellTextCommand(slide, 3, 0, 1, table.Rows[0].Cells[1].Text),
                     new SetTableCellFillCommand(slide, 3, [(3, 2)], new ShapeFill(ShapeFillKind.Solid, "#FF0000")),
                     new SetTableCellFillCommand(slide, 3, [], new ShapeFill(ShapeFillKind.Solid, "#FF0000")),
                     new SetTableOptionsCommand(slide, 3, true, true, null, null, null, null),
                     new SetTableSizesCommand(slide, 3, [1000000, 2000000, 3000000], [400000, 400000, 400000, 400000]),
                     new SplitTableCellCommand(slide, 3, 0, 1),
                     new MergeTableCellsCommand(slide, 3, 0, 1, 1, 1),
                     new MergeTableCellsCommand(slide, 3, 0, 0, 3, 1)
                 })
            presentation.Execute(command);

        Assert.False(presentation.Undo.CanUndo);
        Assert.False(slide.Modified);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void TableCommands_InvalidInput_ThrowWithoutChange()
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var before = SlideCommandTests.Snapshot(presentation);

        Assert.Throws<ArgumentException>(() => presentation.Execute(new InsertTableRowsCommand(slide, 2, 0, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new InsertTableRowsCommand(slide, 3, 5, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new InsertTableColumnsCommand(slide, 3, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new DeleteTableRowsCommand(slide, 3, 3, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new SetTableCellTextCommand(slide, 3, 4, 0, Body("x"))));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTableCellTextCommand(slide, 3, 1, 0, Body("x"))));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTableSizesCommand(slide, 3, [-1, 2, 3], [1, 2, 3, 4])));

        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
    }

    [Theory]
    [InlineData(ShapeFillKind.Gradient, "#FF0000")]
    [InlineData(ShapeFillKind.Picture, null)]
    [InlineData(ShapeFillKind.Other, "#FF0000")]
    [InlineData(ShapeFillKind.Inherited, null)]
    [InlineData(ShapeFillKind.Solid, null)]
    [InlineData(ShapeFillKind.Solid, "scheme:nonsense")]
    public void SetTableCellFill_KindTheWriterCannotWrite_Throws(ShapeFillKind kind, string color)
    {
        var presentation = Deck();
        var slide = presentation.Slides[0];
        var before = SlideCommandTests.Snapshot(presentation);

        var error = Assert.Throws<ArgumentException>(() => presentation.Execute(new SetTableCellFillCommand(slide, 3, [(0, 0)], new ShapeFill(kind, color))));

        Assert.Equal("fill", error.ParamName);
        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
        presentation.Execute(new SetTableCellFillCommand(slide, 3, [(0, 0)], new ShapeFill(ShapeFillKind.None)));
        presentation.Execute(new SetTableCellFillCommand(slide, 3, [(0, 1)], new ShapeFill(ShapeFillKind.Solid, "scheme:accent2")));
        presentation.Execute(new SetTableCellFillCommand(slide, 3, [(0, 1)], null));
        Assert.True(presentation.Undo.CanUndo);
    }
}
