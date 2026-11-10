namespace MudBlazor.Extensions.Office.Spreadsheet;

public sealed partial class CalcEngine
{
    private void AddAnchorsOver(CellNode cell, HashSet<CellNode> affected, List<CellNode> starts)
    {
        foreach (var (anchor, spill) in _spills)
            if (anchor.Sheet == cell.Sheet && anchor != cell && spill.Range.Contains(cell.Cell) && affected.Add(anchor))
                starts.Add(anchor);
    }

    private bool SpillCovers(CellNode anchor, CellRef cell) => _spills.TryGetValue(anchor, out var s) && s.Range.Contains(cell);

    private CellValue Spill(CellNode node, CalcArray array)
    {
        var anchor = node.Cell;
        if ((long)array.Rows * array.Columns > MaxSpillCells || anchor.Row + array.Rows > CellRef.MaxRows
                                                               || anchor.Column + array.Columns > CellRef.MaxColumns)
            return SpillBlocked(node, null);
        var range = CellRange.FromBounds(anchor.Row, anchor.Column, anchor.Row + array.Rows - 1, anchor.Column + array.Columns - 1);
        var sheet = node.Sheet;
        foreach (var c in sheet.ExistingIn(range))
            if (c != anchor && sheet[c] is { HasContent: true } blocker && blocker.SpillAnchor != anchor)
                return SpillBlocked(node, range);

        var old = _spills.TryGetValue(node, out var previous) && previous.Active ? previous.Range : (CellRange?)null;
        if (old is { } oldRange)
            foreach (var c in sheet.ExistingIn(oldRange))
                if (!range.Contains(c) && sheet[c].SpillAnchor == anchor)
                    ClearSpillCell(sheet, c);

        for (var r = 0; r < array.Rows; r++)
            for (var col = 0; col < array.Columns; col++)
            {
                if (r == 0 && col == 0) continue;
                var target = new CellRef(anchor.Row + r, anchor.Column + col);
                var v = array[r, col];
                if (v.Kind == CellValueKind.Empty) v = CellValue.Number(0);
                Touch(sheet, target);
                var cell = sheet.Ensure(target);
                if (cell.SpillAnchor == anchor && cell.Value == v && old?.Contains(target) == true) continue;
                cell.Value = v;
                cell.SpillAnchor = anchor;
                sheet.Sync(target);
                _changed.Add(new CellChange(sheet, target));
                if (old?.Contains(target) != true) _grown.Add(new CellNode(sheet, target.Key));
            }

        _spills[node] = new SpillState(range, true);
        return array[0, 0];
    }

    private CellValue SpillBlocked(CellNode node, CellRange? intended)
    {
        ClearSpill(node);
        if (intended is { } range)
        {
            foreach (var c in node.Sheet.ExistingIn(range))
                if (node.Sheet[c].SpillAnchor == node.Cell)
                    ClearSpillCell(node.Sheet, c);
            _spills[node] = new SpillState(range, false);
        }

        return CellValue.Error(CellError.Spill);
    }

    private void ClearSpill(CellNode node)
    {
        if (!_spills.Remove(node, out var spill) || !spill.Active) return;
        foreach (var c in node.Sheet.ExistingIn(spill.Range))
            if (node.Sheet[c].SpillAnchor == node.Cell)
                ClearSpillCell(node.Sheet, c);
    }

    private void Touch(Worksheet sheet, CellRef c) => _before.TryAdd((sheet, c.Relative.Key), sheet.Capture(c.Relative));

    private void ClearSpillCell(Worksheet sheet, CellRef c)
    {
        Touch(sheet, c);
        var cell = sheet[c];
        sheet.Restore(c, new CellState(default, cell.Formula, cell.StyleId));
        _changed.Add(new CellChange(sheet, c));
        _freed.Add(new CellNode(sheet, c.Key));
    }
}
