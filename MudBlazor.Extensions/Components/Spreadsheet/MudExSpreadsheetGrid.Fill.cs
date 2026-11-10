using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetGrid
{
    private static readonly Regex NumberSuffix = new(@"^(.*?)(\d+)$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Extends the current range to <paramref name="target"/> like dragging the fill handle: numbers, dates and text ending in a
    /// number continue as a linear series, everything else is copied with relative references adjusted.
    /// </summary>
    /// <param name="target">The range to fill; it must contain the current range and extend it in one direction.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    public async Task FillToAsync(CellRange target)
    {
        var src = _selection.Range.Normalized;
        target = target.Normalized;
        if (!CanEdit || target == src || !target.Contains(src.Start) || !target.Contains(src.End)) return;
        var vertical = target.Left == src.Left && target.Right == src.Right;
        if (!vertical && (target.Top != src.Top || target.Bottom != src.Bottom)) return;
        if (target.Area > MaxBulkCells) return;
        if (!await TryWriteAsync(() => Fill(src, target, vertical))) return;
        await SelectAsync(new SpreadsheetSelection(_selection.Active, [target]), _selection.Active);
        StateHasChanged();
    }

    private void Fill(CellRange src, CellRange target, bool vertical)
    {
        var steps = vertical
            ? Enumerable.Range(target.Top, target.RowCount).Where(r => !HidesRow(r)).ToList()
            : Enumerable.Range(target.Left, target.ColumnCount).ToList();
        var sourceSteps = steps.Where(p => vertical ? p >= src.Top && p <= src.Bottom : p >= src.Left && p <= src.Right).ToList();
        if (sourceSteps.Count == 0) return;
        var start = steps.IndexOf(sourceSteps[0]);
        using (_workbook.Undo.BeginTransaction("Fill"))
        {
            var lines = vertical ? src.ColumnCount : src.RowCount;
            for (var line = 0; line < lines; line++)
            {
                if (!vertical && HidesRow(src.Top + line)) continue;
                var seq = sourceSteps.Select(p => vertical ? new CellRef(p, src.Left + line) : new CellRef(src.Top + line, p)).ToList();
                var series = Series(seq);
                for (var i = 0; i < steps.Count; i++)
                {
                    var k = i - start;
                    if (k >= 0 && k < seq.Count) continue;
                    var t = vertical ? new CellRef(steps[i], src.Left + line) : new CellRef(src.Top + line, steps[i]);
                    var s = seq[((k % seq.Count) + seq.Count) % seq.Count];
                    if (series != null)
                    {
                        var (prefix, first, step, digits) = series.Value;
                        var n = first + step * k;
                        _workbook.Execute(new SetValueCommand(_sheet, t, prefix == null
                            ? CellValue.Number(n)
                            : CellValue.Text(prefix + Math.Abs(n).ToString(new string('0', digits), Culture))));
                        CopyStyle(_sheet, s, t);
                    }
                    else
                    {
                        CopyCell(_sheet, s, t);
                    }
                }
            }
        }
    }

    /// <summary>Fills the selection down from its top row (or the cell above a single selected row), like Ctrl+D.</summary>
    public void FillDown() => FillFromEdge(true);

    /// <summary>Fills the selection right from its left column (or the cell left of a single selected column), like Ctrl+R.</summary>
    public void FillRight() => FillFromEdge(false);

    /// <summary>Called by the browser after the fill handle was dragged.</summary>
    /// <param name="top">Top row of the filled range.</param>
    /// <param name="left">Left column of the filled range.</param>
    /// <param name="bottom">Bottom row of the filled range.</param>
    /// <param name="right">Right column of the filled range.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task FillTo(int top, int left, int bottom, int right) => FillToAsync(CellRange.FromBounds(top, left, bottom, right));

    /// <summary>Called by the browser after a double click on the fill handle: fills down along the data in the adjacent column.</summary>
    /// <returns>A task that completes when the cells are written.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task FillHandleDoubleClick()
    {
        var src = _selection.Range.Normalized;
        foreach (var col in new[] { src.Left - 1, src.Right + 1 })
        {
            if (col < 0 || col >= CellRef.MaxColumns) continue;
            var r = src.Bottom;
            while (r + 1 < CellRef.MaxRows && _sheet[new CellRef(r + 1, col)]?.HasContent == true) r++;
            if (r > src.Bottom) return FillToAsync(CellRange.FromBounds(src.Top, src.Left, r, src.Right));
        }

        return Task.CompletedTask;
    }

    private void FillFromEdge(bool down)
    {
        if (!CanEdit) return;
        var r = _selection.Range.Normalized;
        if (r.Area > MaxBulkCells) r = CellRange.FromBounds(r.Top, r.Left, Math.Min(r.Bottom, _sheet.UsedRange.Bottom), Math.Min(r.Right, _sheet.UsedRange.Right));
        var single = down ? r.RowCount == 1 : r.ColumnCount == 1;
        if (single && (down ? r.Top == 0 : r.Left == 0)) return;
        var sourceRow = single ? r.Top - 1 : r.Top;
        while (down && sourceRow > 0 && sourceRow < r.Bottom && HidesRow(sourceRow)) sourceRow += single ? -1 : 1;
        using (_workbook.Undo.BeginTransaction(down ? "Fill down" : "Fill right"))
            foreach (var t in r.Cells())
            {
                if (HidesRow(t.Row)) continue;
                var s = down
                    ? new CellRef(sourceRow, t.Column)
                    : new CellRef(t.Row, single ? r.Left - 1 : r.Left);
                if (s != t) CopyCell(_sheet, s, t);
            }
    }

    private void CopyCell(Worksheet sheet, CellRef from, CellRef to, bool translate = true)
    {
        var src = sheet[from];
        if (src?.Formula != null)
            _workbook.Execute(new SetFormulaCommand(_sheet, to, translate ? ReferenceShifter.Translate(src.Formula, to.Row - from.Row, to.Column - from.Column) : src.Formula));
        else if (src?.HasContent == true)
            _workbook.Execute(new SetValueCommand(_sheet, to, src.Value));
        else if (_sheet[to]?.HasContent == true)
            _workbook.Execute(new ClearCommand(_sheet, new CellRange(to, to)));
        CopyStyle(sheet, from, to);
    }

    private void CopyStyle(Worksheet sheet, CellRef from, CellRef to)
    {
        var id = sheet.EffectiveStyleId(from);
        if (id == _sheet.EffectiveStyleId(to)) return;
        var style = _workbook.Styles[id];
        _workbook.Execute(new SetStyleCommand(_sheet, new CellRange(to, to), style));
    }

    private (string Prefix, double First, double Step, int Digits)? Series(List<CellRef> seq)
    {
        var cells = seq.Select(c => _sheet[c]).ToList();
        if (cells.Any(c => c?.Formula != null || c?.HasContent != true)) return null;
        if (cells.All(c => c.Value.Kind == CellValueKind.Number))
        {
            var first = cells[0].Value.NumberValue;
            if (cells.Count == 1)
                return NumberFormat.IsDateFormat(_workbook.Styles[_sheet.EffectiveStyleId(seq[0])].NumberFormat) ? (null, first, 1, 0) : null;
            return (null, first, (cells[^1].Value.NumberValue - first) / (cells.Count - 1), 0);
        }

        if (!cells.All(c => c.Value.Kind == CellValueKind.Text)) return null;
        var parts = cells.Select(c => NumberSuffix.Match(c.Value.TextValue)).ToList();
        if (parts.Any(p => !p.Success) || parts.Any(p => p.Groups[1].Value != parts[0].Groups[1].Value)) return null;
        var numbers = parts.Select(p => double.Parse(p.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        var step = numbers.Count == 1 ? 1 : (numbers[^1] - numbers[0]) / (numbers.Count - 1);
        return (parts[0].Groups[1].Value, numbers[0], step, parts[0].Groups[2].Value.StartsWith('0') ? parts[0].Groups[2].Value.Length : 1);
    }
}
