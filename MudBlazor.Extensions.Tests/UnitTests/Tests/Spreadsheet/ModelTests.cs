using MudBlazor.Extensions.Office.Spreadsheet;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Spreadsheet;

public class ModelTests
{
    private static CellRef R(string a1) => CellRef.Parse(a1);

    private static CellValue ValueAt(Worksheet ws, string a1) => ws[R(a1)]?.Value ?? default;

    [Theory]
    [InlineData("A1", 0, 0, false, false)]
    [InlineData("$B$2", 1, 1, true, true)]
    [InlineData("XFD1048576", 1048575, 16383, false, false)]
    [InlineData("AA10", 9, 26, false, false)]
    [InlineData("b3", 2, 1, false, false)]
    [InlineData("C$4", 3, 2, true, false)]
    public void CellRef_ParsesAndFormatsA1(string a1, int row, int col, bool rowAbs, bool colAbs)
    {
        var r = CellRef.Parse(a1);
        Assert.Equal(new CellRef(row, col, rowAbs, colAbs), r);
        Assert.Equal(a1.ToUpperInvariant(), r.ToA1());
    }

    [Theory]
    [InlineData("")]
    [InlineData("A0")]
    [InlineData("XFE1")]
    [InlineData("A1048577")]
    [InlineData("1A")]
    [InlineData("A1B")]
    [InlineData("$")]
    [InlineData("A")]
    [InlineData("5")]
    public void CellRef_RejectsInvalidInput(string a1)
    {
        Assert.False(CellRef.TryParse(a1, out _));
        Assert.Throws<FormatException>(() => CellRef.Parse(a1));
    }

    [Fact]
    public void CellRef_ColumnNames()
    {
        Assert.Equal("A", CellRef.ColumnName(0));
        Assert.Equal("Z", CellRef.ColumnName(25));
        Assert.Equal("AA", CellRef.ColumnName(26));
        Assert.Equal("XFD", CellRef.ColumnName(16383));
    }

    [Fact]
    public void CellRange_ParsesWholeColumnsAndRows()
    {
        var col = CellRange.Parse("A:A");
        Assert.Equal(new CellRef(0, 0), col.Start);
        Assert.Equal(new CellRef(1048575, 0), col.End);
        Assert.Equal("A:A", col.ToA1());

        var row = CellRange.Parse("3:3");
        Assert.Equal(new CellRef(2, 0), row.Start);
        Assert.Equal(new CellRef(2, 16383), row.End);
        Assert.Equal("3:3", row.ToA1());
        Assert.True(row.Contains(R("XFD3")));
        Assert.False(row.Contains(R("A4")));

        var cols = CellRange.Parse("$B:$D");
        Assert.True(cols.Contains(R("C500")));
        Assert.Equal("$B:$D", cols.ToA1());
    }

    [Fact]
    public void CellRange_ParsesRectanglesAndNormalizes()
    {
        var range = CellRange.Parse("B2:A1");
        Assert.True(range.Contains(R("A2")));
        Assert.False(range.Contains(R("C1")));
        Assert.Equal(new[] { "A1", "B1", "A2", "B2" }, range.Cells().Select(c => c.ToA1()));
        Assert.Equal("C3", CellRange.Parse("C3").ToA1());
        Assert.Equal("A1:B2", CellRange.Parse("A1:B2").ToA1());
        Assert.False(CellRange.TryParse("A:3", out _));
        Assert.False(CellRange.TryParse("A1:B2:C3", out _));
    }

    [Fact]
    public void CellKeys_DoNotCollideAtLimits()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, new CellRef(0, 0), CellValue.Number(1)));
        wb.Execute(new SetValueCommand(ws, new CellRef(1, 0), CellValue.Number(2)));
        wb.Execute(new SetValueCommand(ws, new CellRef(0, 16383), CellValue.Number(3)));
        wb.Execute(new SetValueCommand(ws, new CellRef(1048575, 16383), CellValue.Number(4)));
        Assert.Equal(4, ws.UsedCells().Count());
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "XFD1"));
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "XFD1048576"));
        Assert.Equal(CellRange.Parse("A1:XFD1048576"), ws.UsedRange);
    }

    [Fact]
    public void CellValue_KindsAndEquality()
    {
        Assert.Equal(CellValueKind.Empty, default(CellValue).Kind);
        Assert.Equal(CellValue.Number(1.5), CellValue.Number(1.5));
        Assert.NotEqual(CellValue.Number(1), CellValue.Boolean(true));
        Assert.Equal(CellError.Div0, CellValue.Error(CellError.Div0).ErrorValue);
        Assert.Equal("#DIV/0!", CellValue.Error(CellError.Div0).ToString());
        Assert.Equal("TRUE", CellValue.Boolean(true).ToString());
        Assert.Equal("x", CellValue.Text("x").TextValue);
    }

    [Fact]
    public void Styles_AreDeduplicated()
    {
        var table = Workbook.Create().Styles;
        var bold = CellStyle.Default with { Font = new CellFont { Bold = true } };
        var bold2 = CellStyle.Default with { Font = new CellFont { Bold = true } };
        var red = CellStyle.Default with { Fill = new CellFill { Color = "#FF0000" } };

        var a = table.GetOrAdd(bold);
        Assert.Equal(a, table.GetOrAdd(bold2));
        Assert.NotEqual(a, table.GetOrAdd(red));
        Assert.Equal(0, table.GetOrAdd(CellStyle.Default));
        Assert.Equal(3, table.Count);
        Assert.Equal(bold, table[a]);
    }

    [Fact]
    public void SetValue_UndoRedo_RestoresPreviousValueAndStyle()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var a1 = R("A1");
        wb.Execute(new SetStyleCommand(ws, new CellRange(a1, a1), CellStyle.Default with { Font = new CellFont { Bold = true } }));
        wb.Execute(new SetValueCommand(ws, a1, CellValue.Number(1)));
        wb.Execute(new SetValueCommand(ws, a1, CellValue.Text("two")));
        var boldId = ws[a1].StyleId;
        Assert.NotEqual(0, boldId);

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(1), ws[a1].Value);
        Assert.Equal(boldId, ws[a1].StyleId);
        wb.Undo.Undo();
        Assert.Equal(CellValueKind.Empty, ws[a1].Value.Kind);
        Assert.Equal(boldId, ws[a1].StyleId);
        wb.Undo.Undo();
        Assert.Null(ws[a1]);
        Assert.False(wb.Undo.CanUndo);

        wb.Undo.Redo();
        wb.Undo.Redo();
        wb.Undo.Redo();
        Assert.Equal(CellValue.Text("two"), ws[a1].Value);
        Assert.Equal(boldId, ws[a1].StyleId);
        Assert.False(wb.Undo.CanRedo);

        wb.Undo.Undo();
        wb.Execute(new SetValueCommand(ws, a1, CellValue.Number(9)));
        Assert.False(wb.Undo.CanRedo);
    }

    [Fact]
    public void SetFormula_StoresTextWithoutLeadingEqualsAndUndoes()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Number(5)));
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "=SUM(A1:A3)"));
        Assert.Equal("SUM(A1:A3)", ws[R("B1")].Formula);
        wb.Undo.Undo();
        Assert.Null(ws[R("B1")].Formula);
        Assert.Equal(CellValue.Number(5), ws[R("B1")].Value);
        wb.Execute(new SetFormulaCommand(ws, R("B1"), "1+1"));
        wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Number(7)));
        Assert.Null(ws[R("B1")].Formula);
    }

    [Fact]
    public void Clear_KeepsOrDropsStyleAndUndoes()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var range = CellRange.Parse("A1:B2");
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetStyleCommand(ws, range, CellStyle.Default with { NumberFormat = "0.00" }));
        var styled = ws[R("A1")].StyleId;

        wb.Execute(new ClearCommand(ws, range));
        Assert.Equal(CellValueKind.Empty, ws[R("A1")].Value.Kind);
        Assert.Equal(styled, ws[R("A1")].StyleId);

        wb.Execute(new ClearCommand(ws, range, clearStyle: true));
        Assert.Empty(ws.UsedCells());

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(1), ws[R("A1")].Value);
        Assert.Equal(styled, ws[R("B2")].StyleId);
    }

    [Fact]
    public void Transaction_IsOneUndoStep()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var events = new List<WorkbookChangedEventArgs>();
        wb.Changed += (_, e) => events.Add(e);

        using (wb.Undo.BeginTransaction("Paste"))
        {
            wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
            wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(2)));
            wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Number(3)));
            Assert.Empty(events);
            Assert.False(wb.Undo.CanUndo);
        }

        Assert.Single(events);
        Assert.Equal(3, events[0].Cells.Count);
        Assert.Equal("Paste", wb.Undo.UndoDescription);

        wb.Undo.Undo();
        Assert.Empty(ws.UsedCells());
        Assert.False(wb.Undo.CanUndo);
        Assert.Equal(2, events.Count);
        Assert.Equal(3, events[1].Cells.Count);

        wb.Undo.Redo();
        Assert.Equal(3, ws.UsedCells().Count());
    }

    [Fact]
    public void Transaction_NestedMergesIntoOuterAndEmptyIsIgnored()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        using (wb.Undo.BeginTransaction("Nothing")) { }
        Assert.False(wb.Undo.CanUndo);

        using (wb.Undo.BeginTransaction("Outer"))
        {
            wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
            using (wb.Undo.BeginTransaction("Inner"))
                wb.Execute(new SetValueCommand(ws, R("A2"), CellValue.Number(2)));
        }

        wb.Undo.Undo();
        Assert.Empty(ws.UsedCells());
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public void InsertRows_ShiftsCellsMergesAndFreeze()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new MergeCommand(ws, CellRange.Parse("A2:B3")));
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetValueCommand(ws, R("A3"), CellValue.Number(3)));
        wb.Execute(new FreezeCommand(ws, R("B4")));
        wb.Execute(new ResizeCommand(ws, Axis.Row, 2, 1, 40));
        wb.Execute(new SetHiddenCommand(ws, Axis.Row, 4, 1, true));
        var events = new List<WorkbookChangedEventArgs>();
        wb.Changed += (_, e) => events.Add(e);

        wb.Execute(new InsertRowsCommand(ws, 1, 2));

        Assert.Equal(CellValue.Number(1), ValueAt(ws, "A1"));
        Assert.Equal(CellValueKind.Empty, ValueAt(ws, "A3").Kind);
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "A5"));
        Assert.Equal(new[] { CellRange.Parse("A4:B5") }, ws.Merges);
        Assert.Equal(R("B6"), ws.FreezeAt);
        Assert.Equal(40, ws.RowHeight(4));
        Assert.Equal(ws.DefaultRowHeight, ws.RowHeight(2));
        Assert.True(ws.IsRowHidden(6));
        Assert.False(ws.IsRowHidden(4));
        var change = Assert.Single(Assert.Single(events).Structure);
        Assert.Equal(new StructureChange(StructureChangeKind.RowsInserted, ws, 1, 2, null), change);

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "A3"));
        Assert.Equal(new[] { CellRange.Parse("A2:B3") }, ws.Merges);
        Assert.Equal(R("B4"), ws.FreezeAt);
        Assert.Equal(40, ws.RowHeight(2));
        Assert.True(ws.IsRowHidden(4));
    }

    [Fact]
    public void InsertRows_AtFreezeLineKeepsFreeze()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new FreezeCommand(ws, R("A2")));
        wb.Execute(new InsertRowsCommand(ws, 1, 3));
        Assert.Equal(R("A2"), ws.FreezeAt);
        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        Assert.Equal(R("A3"), ws.FreezeAt);
    }

    [Fact]
    public void InsertRows_BeyondLimitThrowsAndLeavesSheetUntouched()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var last = new CellRef(CellRef.MaxRows - 1, 0);
        wb.Execute(new SetValueCommand(ws, last, CellValue.Number(1)));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new InsertRowsCommand(ws, 0, 1)));
        Assert.Equal(CellValue.Number(1), ws[last].Value);
        wb.Undo.Undo();
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public void InsertRows_BeyondLimitLeavesSpillAnchorsUntouched()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        using var engine = new CalcEngine(wb);
        wb.Execute(new SetFormulaCommand(ws, new CellRef(1, 0), "=SEQUENCE(3)"));
        var last = new CellRef(CellRef.MaxRows - 1, 0);
        wb.Execute(new SetValueCommand(ws, last, CellValue.Number(1)));
        var anchor = ws[new CellRef(2, 0)]!.SpillAnchor;
        Assert.NotNull(anchor);
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new InsertRowsCommand(ws, 0, 1)));
        Assert.Equal(anchor, ws[new CellRef(2, 0)]!.SpillAnchor);
        Assert.Equal(CellValue.Number(1), ws[last].Value);
    }

    [Fact]
    public void DeleteRows_RemovesCellsShrinksMergesAndUndoes()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new MergeCommand(ws, CellRange.Parse("A2:B3")));
        wb.Execute(new MergeCommand(ws, CellRange.Parse("A4:B6")));
        for (var i = 1; i <= 5; i++)
            wb.Execute(new SetValueCommand(ws, R("A" + i), CellValue.Number(i)));
        wb.Execute(new FreezeCommand(ws, R("A5")));

        wb.Execute(new DeleteRowsCommand(ws, 1, 3));

        Assert.Equal(CellValue.Number(1), ValueAt(ws, "A1"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "A2"));
        Assert.Equal(CellValueKind.Empty, ValueAt(ws, "A3").Kind);
        Assert.Equal(new[] { CellRange.Parse("A2:B3") }, ws.Merges);
        Assert.Equal(R("A2"), ws.FreezeAt);

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(2), ValueAt(ws, "A2"));
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "A4"));
        Assert.Equal(CellValue.Number(5), ValueAt(ws, "A5"));
        Assert.Equal(new[] { CellRange.Parse("A2:B3"), CellRange.Parse("A4:B6") }, ws.Merges);
        Assert.Equal(R("A5"), ws.FreezeAt);
    }

    [Fact]
    public void DeleteColumns_RemovesCellsAndShiftsRight()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        string[] cols = ["A", "B", "C", "D"];
        for (var i = 0; i < cols.Length; i++)
            wb.Execute(new SetValueCommand(ws, R(cols[i] + "1"), CellValue.Number(i + 1)));
        wb.Execute(new ResizeCommand(ws, Axis.Column, 2, 1, 120));
        wb.Execute(new ResizeCommand(ws, Axis.Column, 3, 1, 90));

        wb.Execute(new DeleteColumnsCommand(ws, 1, 2));

        Assert.Equal(CellValue.Number(1), ValueAt(ws, "A1"));
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "B1"));
        Assert.Equal(CellValueKind.Empty, ValueAt(ws, "C1").Kind);
        Assert.Equal(2, ws.UsedCells().Count());
        Assert.Equal(90, ws.ColumnWidth(1));
        Assert.Equal(ws.DefaultColumnWidth, ws.ColumnWidth(2));

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(2), ValueAt(ws, "B1"));
        Assert.Equal(CellValue.Number(3), ValueAt(ws, "C1"));
        Assert.Equal(CellValue.Number(4), ValueAt(ws, "D1"));
        Assert.Equal(120, ws.ColumnWidth(2));
        Assert.Equal(90, ws.ColumnWidth(3));
    }

    [Fact]
    public void Layout_HideResizeMergeFreezeUndo()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetHiddenCommand(ws, Axis.Column, 1, 2, true));
        Assert.True(ws.IsColumnHidden(1) && ws.IsColumnHidden(2));
        wb.Execute(new SetHiddenCommand(ws, Axis.Column, 2, 1, false));
        Assert.False(ws.IsColumnHidden(2));
        wb.Undo.Undo();
        Assert.True(ws.IsColumnHidden(2));
        wb.Undo.Undo();
        Assert.False(ws.IsColumnHidden(1));

        wb.Execute(new ResizeCommand(ws, Axis.Row, 0, 2, 33));
        Assert.Equal(33, ws.RowHeight(1));
        wb.Execute(new ResizeCommand(ws, Axis.Row, 0, 2, null));
        Assert.Empty(ws.RowHeights);
        wb.Undo.Undo();
        Assert.Equal(33, ws.RowHeight(0));

        wb.Execute(new MergeCommand(ws, CellRange.Parse("A1:C1")));
        wb.Execute(new MergeCommand(ws, CellRange.Parse("B1:D2")));
        Assert.Equal(new[] { CellRange.Parse("B1:D2") }, ws.Merges);
        wb.Execute(new UnmergeCommand(ws, CellRange.Parse("C2")));
        Assert.Empty(ws.Merges);
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(new[] { CellRange.Parse("A1:C1") }, ws.Merges);

        wb.Execute(new FreezeCommand(ws, R("C3")));
        Assert.Equal(R("C3"), ws.FreezeAt);
        wb.Execute(new FreezeCommand(ws, null));
        Assert.Null(ws.FreezeAt);
        wb.Undo.Undo();
        Assert.Equal(R("C3"), ws.FreezeAt);
    }

    [Fact]
    public void Sheets_AddRenameMoveColorDeleteUndo()
    {
        var wb = Workbook.Create("One");
        var add = new AddSheetCommand("Two");
        wb.Execute(add);
        wb.Execute(new AddSheetCommand("Three", 0));
        Assert.Equal(new[] { "Three", "One", "Two" }, wb.Sheets.Select(s => s.Name));

        wb.Execute(new MoveSheetCommand(add.Sheet, 0));
        Assert.Equal(new[] { "Two", "Three", "One" }, wb.Sheets.Select(s => s.Name));
        wb.Execute(new SetTabColorCommand(add.Sheet, "#00FF00"));
        Assert.Equal("#00FF00", add.Sheet.TabColor);
        wb.Execute(new DeleteSheetCommand(add.Sheet));
        Assert.Equal(new[] { "Three", "One" }, wb.Sheets.Select(s => s.Name));

        wb.Undo.Undo();
        Assert.Same(add.Sheet, wb.Sheets[0]);
        Assert.Equal("#00FF00", add.Sheet.TabColor);
        wb.Undo.Undo();
        Assert.Null(add.Sheet.TabColor);
        wb.Undo.Undo();
        Assert.Equal(new[] { "Three", "One", "Two" }, wb.Sheets.Select(s => s.Name));
        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal(new[] { "One" }, wb.Sheets.Select(s => s.Name));
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new DeleteSheetCommand(wb.Sheets[0])));
    }

    [Theory]
    [InlineData("")]
    [InlineData("one")]
    [InlineData("a/b")]
    [InlineData("[x]")]
    [InlineData("12345678901234567890123456789012")]
    public void Sheets_RejectInvalidOrDuplicateNames(string name)
    {
        var wb = Workbook.Create("One");
        Assert.Throws<ArgumentException>(() => wb.Execute(new AddSheetCommand(name)));
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public void Sheets_RenameRejectsOtherSheetsNameButAllowsCaseChange()
    {
        var wb = Workbook.Create("One");
        wb.Execute(new AddSheetCommand("Two"));
        Assert.Throws<ArgumentException>(() => wb.Execute(new RenameSheetCommand(wb.Sheets[0], "TWO")));
        wb.Execute(new RenameSheetCommand(wb.Sheets[0], "ONE"));
        Assert.Equal("ONE", wb.Sheets[0].Name);
    }

    [Fact]
    public void SheetRename_UpdatesDefinedNames()
    {
        var wb = Workbook.Create("Sheet1");
        wb.Execute(new DefineNameCommand("Total", "Sheet1!$A$1:$B$2"));
        wb.Execute(new DefineNameCommand("Other", "Sheet10!$A$1+MySheet1!$B$1"));

        wb.Execute(new RenameSheetCommand(wb.Sheets[0], "My Data"));
        Assert.Equal("'My Data'!$A$1:$B$2", wb.DefinedNames["Total"]);
        Assert.Equal("Sheet10!$A$1+MySheet1!$B$1", wb.DefinedNames["other"]);

        wb.Execute(new RenameSheetCommand(wb.Sheets[0], "Data"));
        Assert.Equal("Data!$A$1:$B$2", wb.DefinedNames["Total"]);

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Equal("Sheet1", wb.Sheets[0].Name);
        Assert.Equal("Sheet1!$A$1:$B$2", wb.DefinedNames["Total"]);

        wb.Undo.Undo();
        wb.Undo.Undo();
        Assert.Empty(wb.DefinedNames);
    }

    [Fact]
    public void Changed_ReportsRenameWithOldName()
    {
        var wb = Workbook.Create("A");
        var events = new List<WorkbookChangedEventArgs>();
        wb.Changed += (_, e) => events.Add(e);
        wb.Execute(new RenameSheetCommand(wb.Sheets[0], "B"));
        var change = Assert.Single(Assert.Single(events).Structure);
        Assert.Equal(StructureChangeKind.SheetRenamed, change.Kind);
        Assert.Equal("A", change.OldName);
        Assert.Equal("B", change.Sheet.Name);
    }

    [Fact]
    public void StyleWholeColumn_DoesNotMaterialiseCells()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        var bold = CellStyle.Default with { Font = new CellFont { Bold = true } };

        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A:A"), bold));

        Assert.Single(ws.UsedCells());
        var id = wb.Styles.GetOrAdd(bold);
        Assert.Equal(id, ws.ColumnStyle(0));
        Assert.Equal(id, ws.EffectiveStyleId(R("A500000")));
        Assert.Equal(id, ws[R("A1")].StyleId);
        Assert.Equal(0, ws.EffectiveStyleId(R("B1")));

        wb.Undo.Undo();
        Assert.Equal(0, ws.ColumnStyle(0));
        Assert.Equal(0, ws[R("A1")].StyleId);
        wb.Undo.Redo();
        Assert.Equal(id, ws.ColumnStyle(0));
    }

    [Fact]
    public void StyleWholeRow_SetsRowStyleAndRestylesExistingCellsOnly()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("C2"), CellValue.Number(1)));
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("2:2"), CellStyle.Default with { NumberFormat = "0.0" }));
        Assert.Single(ws.UsedCells());
        Assert.NotEqual(0, ws.RowStyle(1));
        Assert.Equal(ws.RowStyle(1), ws[R("C2")].StyleId);
        Assert.Equal(ws.RowStyle(1), ws.EffectiveStyleId(R("XFD2")));
    }

    [Fact]
    public void InsertRows_AfterWholeColumnStyle_Succeeds()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var bold = CellStyle.Default with { Font = new CellFont { Bold = true } };
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("A:A"), bold));
        var last = new CellRef(CellRef.MaxRows - 1, 1);
        wb.Execute(new SetStyleCommand(ws, new CellRange(last, last), bold));
        Assert.Single(ws.UsedCells());

        wb.Execute(new InsertRowsCommand(ws, 0, 1));

        Assert.Empty(ws.UsedCells());
        Assert.Equal(wb.Styles.GetOrAdd(bold), ws.ColumnStyle(0));

        wb.Undo.Undo();
        Assert.Equal(wb.Styles.GetOrAdd(bold), ws[last].StyleId);
    }

    [Fact]
    public void EffectiveStyle_CellOverridesRowOverridesColumn()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        var red = CellStyle.Default with { Fill = new CellFill { Color = "#FF0000" } };
        var bold = CellStyle.Default with { Font = new CellFont { Bold = true } };
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("B:B"), red));
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("2:2"), bold));
        var redId = wb.Styles.GetOrAdd(red);
        var boldId = wb.Styles.GetOrAdd(bold);

        Assert.Equal(redId, ws.EffectiveStyleId(R("B1")));
        Assert.Equal(boldId, ws.EffectiveStyleId(R("B2")));
        Assert.Equal(boldId, ws.EffectiveStyleId(R("A2")));
        Assert.Equal(0, ws.EffectiveStyleId(R("A1")));

        wb.Execute(new SetStyleCommand(ws, new CellRange(R("B2"), R("B2")), s => s with { NumberFormat = "0%" }));
        var own = wb.Styles[ws.EffectiveStyleId(R("B2"))];
        Assert.True(own.Font.Bold);
        Assert.Equal("0%", own.NumberFormat);
        Assert.Equal(boldId, ws.EffectiveStyleId(R("C2")));
    }

    [Fact]
    public void InsertRow_ChangedEventCarriesNoPerCellFlood()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        using (wb.Undo.BeginTransaction("Fill"))
            for (var i = 0; i < 1000; i++)
                wb.Execute(new SetValueCommand(ws, new CellRef(i, 0), CellValue.Number(i)));
        var events = new List<WorkbookChangedEventArgs>();
        wb.Changed += (_, e) => events.Add(e);

        wb.Execute(new InsertRowsCommand(ws, 0, 1));
        wb.Execute(new DeleteRowsCommand(ws, 0, 1));
        wb.Undo.Undo();
        wb.Undo.Undo();

        Assert.Equal(4, events.Count);
        Assert.All(events, e => Assert.Empty(e.Cells));
        Assert.All(events, e => Assert.Single(e.Structure));
    }

    [Fact]
    public void Resize_WholeAxisSetsDefaultInsteadOfEntries()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new ResizeCommand(ws, Axis.Row, 3, 1, 50));
        wb.Execute(new ResizeCommand(ws, Axis.Row, 0, CellRef.MaxRows, 30));
        Assert.Empty(ws.RowHeights);
        Assert.Equal(30, ws.DefaultRowHeight);
        Assert.Equal(30, ws.RowHeight(3));

        wb.Undo.Undo();
        Assert.Equal(Worksheet.StandardRowHeight, ws.DefaultRowHeight);
        Assert.Equal(50, ws.RowHeight(3));

        wb.Execute(new ResizeCommand(ws, Axis.Column, 0, CellRef.MaxColumns, 80));
        Assert.Equal(80, ws.ColumnWidth(5));
    }

    [Fact]
    public void AxisCommands_RejectOutOfRangeIndexes()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new ResizeCommand(ws, Axis.Column, CellRef.MaxColumns, 1, 10)));
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new ResizeCommand(ws, Axis.Row, -1, 1, 10)));
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new SetHiddenCommand(ws, Axis.Row, 0, CellRef.MaxRows + 1, true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new DeleteColumnsCommand(ws, 16383, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new InsertRowsCommand(ws, CellRef.MaxRows, 1)));
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public void Refs_OutsideLimitsAreRejected()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        Assert.Throws<ArgumentOutOfRangeException>(() => CellRef.ColumnName(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CellRef.ColumnName(CellRef.MaxColumns));
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new SetValueCommand(ws, new CellRef(0, CellRef.MaxColumns), CellValue.Number(1))));
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new SetValueCommand(ws, new CellRef(-1, 0), CellValue.Number(1))));
        Assert.Throws<ArgumentOutOfRangeException>(() => wb.Execute(new FreezeCommand(ws, new CellRef(CellRef.MaxRows, 0))));
        Assert.Null(ws[new CellRef(0, CellRef.MaxColumns)]);
        Assert.False(wb.Undo.CanUndo);
    }

    [Fact]
    public void Merge_KeepsOnlyTopLeftValueAndUndoRestores()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetValueCommand(ws, R("A1"), CellValue.Number(1)));
        wb.Execute(new SetValueCommand(ws, R("B1"), CellValue.Number(2)));
        wb.Execute(new SetFormulaCommand(ws, R("A2"), "A1+1"));

        wb.Execute(new MergeCommand(ws, CellRange.Parse("A1:B2")));
        Assert.Equal(CellValue.Number(1), ws[R("A1")].Value);
        Assert.Null(ws[R("B1")]);
        Assert.Null(ws[R("A2")]);

        wb.Undo.Undo();
        Assert.Equal(CellValue.Number(2), ws[R("B1")].Value);
        Assert.Equal("A1+1", ws[R("A2")].Formula);
        Assert.Empty(ws.Merges);
    }

    [Fact]
    public void UsedRange_IgnoresStyleOnlyCellsAndTracksChanges()
    {
        var wb = Workbook.Create();
        var ws = wb.Sheets[0];
        wb.Execute(new SetStyleCommand(ws, CellRange.Parse("Z100"), CellStyle.Default with { NumberFormat = "0" }));
        Assert.Equal(CellRange.Parse("A1"), ws.UsedRange);
        wb.Execute(new SetValueCommand(ws, R("C3"), CellValue.Number(1)));
        Assert.Equal(CellRange.Parse("C3"), ws.UsedRange);
        wb.Execute(new SetValueCommand(ws, R("D5"), CellValue.Number(1)));
        Assert.Equal(CellRange.Parse("C3:D5"), ws.UsedRange);
        wb.Undo.Undo();
        Assert.Equal(CellRange.Parse("C3"), ws.UsedRange);
        wb.Execute(new InsertRowsCommand(ws, 0, 2));
        Assert.Equal(CellRange.Parse("C5"), ws.UsedRange);
    }

    [Fact]
    public void DeleteSheet_RejectsForeignSheet()
    {
        var wb = Workbook.Create();
        var other = Workbook.Create("Other");
        Assert.Throws<InvalidOperationException>(() => wb.Execute(new DeleteSheetCommand(other.Sheets[0])));
    }

    [Fact]
    public void Undo_KeepsStepWhenRevertFails()
    {
        var wb = Workbook.Create();
        var failing = new FailingCommand();
        wb.Execute(failing);
        Assert.Throws<InvalidOperationException>(() => wb.Undo.Undo());
        Assert.True(wb.Undo.CanUndo);
        Assert.False(wb.Undo.CanRedo);
        failing.Fail = false;
        wb.Undo.Undo();
        Assert.True(wb.Undo.CanRedo);
    }

    private sealed class FailingCommand : IWorkbookCommand
    {
        public bool Fail { get; set; } = true;

        public string Description => "Failing";

        public void Apply(Workbook wb)
        {
        }

        public void Revert(Workbook wb)
        {
            if (Fail) throw new InvalidOperationException();
        }
    }
}
