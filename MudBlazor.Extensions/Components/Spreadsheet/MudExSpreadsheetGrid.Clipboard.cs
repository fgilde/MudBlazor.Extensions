using System.ComponentModel;
using System.Text;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetGrid
{
    private const long MaxPasteBytes = HtmlTableFormat.MaxLength;

    private ClipboardSource _clipboard;

    /// <summary>
    /// Copies the current range to the clipboard as tab separated text and as an HTML table with formats. Like Excel, only the visible
    /// rows of a filtered range are copied.
    /// </summary>
    /// <returns>A task that completes when the data is handed to the browser.</returns>
    public Task CopyAsync() => CopyCoreAsync(false);

    /// <summary>Copies the current range and moves it on the next paste.</summary>
    /// <returns>A task that completes when the data is handed to the browser.</returns>
    public Task CutAsync() => CopyCoreAsync(CanEdit);

    /// <summary>
    /// Pastes text at the top-left cell of the selection. Tab separated text fills a range; each value is parsed like typed input.
    /// Text that was copied from this grid pastes the original cells with formulas, adjusting relative references; cut cells are
    /// moved with their formulas unchanged.
    /// </summary>
    /// <param name="text">The clipboard text.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    public Task PasteTextAsync(string text) => PasteAsync(text, null);

    /// <summary>
    /// Pastes clipboard data at the top-left cell of the selection. A table in <paramref name="html"/>, as Excel, Google Sheets and
    /// browsers copy it, wins over the text and brings values, formats and merged cells; data copied from this grid pastes the original cells.
    /// </summary>
    /// <param name="text">The <c>text/plain</c> part of the clipboard.</param>
    /// <param name="html">The <c>text/html</c> part of the clipboard, or null.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    public async Task PasteAsync(string text, string html)
    {
        if (!CanEdit || (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(html))) return;
        var origin = _selection.Range.Normalized;
        CellRange target = default;
        if (!await TryWriteAsync(() => target = Paste(text ?? "", html, origin))) return;
        await SelectAsync(new SpreadsheetSelection(target.Start, [target]), target.Start);
        StateHasChanged();
    }

    /// <summary>Called by the browser on paste. The data arrives as streams, so large clipboards do not hit the message size limit of Blazor Server.</summary>
    /// <param name="data">The clipboard text as UTF-8.</param>
    /// <param name="html">The HTML of the clipboard as UTF-8, or null.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task PasteStreamAsync(IJSStreamReference data, IJSStreamReference html = null)
    {
        string text, markup;
        try
        {
            text = await ReadTextAsync(data);
            markup = await ReadTextAsync(html);
        }
        catch (Exception e)
        {
            await ReportErrorAsync(e);
            return;
        }

        await PasteAsync(text, markup);
    }

    internal async Task<string> ReadClipboardAsync(string type = "text/plain")
    {
        if (JsReference is not { } js) return null;
        return await ReadTextAsync(await js.InvokeAsync<IJSStreamReference>("readClipboard", type));
    }

    private static async Task<string> ReadTextAsync(IJSStreamReference data)
    {
        if (data == null) return null;
        await using (data)
        {
            await using var stream = await data.OpenReadStreamAsync(MaxPasteBytes);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }

    private CellRange Paste(string text, string html, CellRange origin)
    {
        if (_clipboard is { } clip && clip.Text == text && _workbook.Sheets.Contains(clip.Sheet)) return PasteCopied(clip, origin);
        if (!string.IsNullOrEmpty(html) && HtmlTableFormat.Read(html, Culture) is { } table) return PasteTable(table, origin);
        var rows = ParseTabular(text);
        if (rows.Count == 0) return new CellRange(origin.Start, origin.Start);
        var width = rows.Max(r => r.Count);
        var target = Target(origin, rows.Count, width);
        using (_workbook.Undo.BeginTransaction("Paste"))
            for (var i = 0; i < rows.Count; i++)
                for (var j = 0; j < rows[i].Count; j++)
                {
                    var t = new CellRef(origin.Top + i, origin.Left + j);
                    if (target.Contains(t)) WriteInput(t, rows[i][j]);
                }

        return target;
    }

    private static CellRange Target(CellRange origin, int rows, int columns) =>
        CellRange.FromBounds(origin.Top, origin.Left, Math.Min(CellRef.MaxRows - 1, origin.Top + rows - 1), Math.Min(CellRef.MaxColumns - 1, origin.Left + columns - 1));

    private CellRange PasteCopied(ClipboardSource clip, CellRange origin)
    {
        var target = Target(origin, clip.Rows.Count, clip.Range.ColumnCount);
        var cells = new List<(CellRef Source, CellRef Target)>();
        for (var i = 0; i < clip.Rows.Count; i++)
            for (var c = clip.Range.Left; c <= clip.Range.Right; c++)
            {
                var t = new CellRef(origin.Top + i, c - clip.Range.Left + target.Left);
                if (target.Contains(t)) cells.Add((new CellRef(clip.Rows[i], c), t));
            }

        var dRow = target.Top - clip.Range.Top;
        if (clip.Sheet == _sheet && (dRow > 0 || (dRow == 0 && target.Left > clip.Range.Left))) cells.Reverse();
        using (_workbook.Undo.BeginTransaction(clip.Cut ? "Move" : "Paste"))
        {
            foreach (var (s, t) in cells) CopyCell(clip.Sheet, s, t, !clip.Cut);
            if (clip.Cut)
                foreach (var row in clip.Rows)
                    for (var c = clip.Range.Left; c <= clip.Range.Right; c++)
                    {
                        var s = new CellRef(row, c);
                        if (clip.Sheet != _sheet || !target.Contains(s)) ClearCell(clip.Sheet, s, true);
                    }
        }

        if (clip.Cut) _clipboard = null;
        return target;
    }

    private CellRange PasteTable(HtmlTable table, CellRange origin)
    {
        var target = Target(origin, table.RowCount, table.ColumnCount);
        using (_workbook.Undo.BeginTransaction("Paste"))
        {
            if (_sheet.Merges.Any(m => m.Intersects(target))) _workbook.Execute(new UnmergeCommand(_sheet, target));
            foreach (var cell in table.Cells)
            {
                var t = new CellRef(origin.Top + cell.Row, origin.Left + cell.Column);
                if (!target.Contains(t)) continue;
                var h = cell.Style;
                _workbook.Execute(new SetStyleCommand(_sheet, new CellRange(t, t), s => s with
                {
                    Font = s.Font with { Bold = h.Font.Bold, Italic = h.Font.Italic, Underline = h.Font.Underline, Strikethrough = h.Font.Strikethrough, Color = h.Font.Color },
                    Fill = h.Fill,
                    Alignment = s.Alignment with { Horizontal = h.Alignment.Horizontal, Vertical = h.Alignment.Vertical, WrapText = h.Alignment.WrapText },
                    NumberFormat = h.NumberFormat
                }));
                if (cell.Formula != null)
                    _workbook.Execute(new SetFormulaCommand(_sheet, t, cell.Source is { } s ? ReferenceShifter.Translate(cell.Formula, t.Row - s.Row, t.Column - s.Column) : cell.Formula));
                else if (cell.Value.Kind != CellValueKind.Empty) _workbook.Execute(new SetValueCommand(_sheet, t, cell.Value));
                else if (_sheet[t]?.HasContent == true) _workbook.Execute(new ClearCommand(_sheet, new CellRange(t, t)));
                var merge = CellRange.FromBounds(t.Row, t.Column, Math.Min(target.Bottom, t.Row + cell.RowSpan - 1), Math.Min(target.Right, t.Column + cell.ColumnSpan - 1));
                if (!merge.IsSingleCell) _workbook.Execute(new MergeCommand(_sheet, merge));
            }
        }

        return target;
    }

    internal Task PasteValuesAsync(string text)
    {
        _clipboard = null;
        return PasteTextAsync(text);
    }

    private async Task CopyCoreAsync(bool cut)
    {
        if (_sheet == null) return;
        var range = _selection.Range.Normalized;
        var used = _sheet.UsedRange;
        if (range.Area > MaxBulkCells)
            range = CellRange.FromBounds(range.Top, range.Left, Math.Max(range.Top, Math.Min(range.Bottom, used.Bottom)), Math.Max(range.Left, Math.Min(range.Right, used.Right)));
        var rows = (_sheet.FilteredRows.Count > 0 ? _sheet.VisibleRowsIn(range) : Enumerable.Range(range.Top, range.RowCount)).ToList();
        var sb = new StringBuilder();
        foreach (var r in rows)
        {
            for (var c = range.Left; c <= range.Right; c++)
            {
                if (c > range.Left) sb.Append('\t');
                var text = DisplayText(new CellRef(r, c));
                sb.Append(text.IndexOfAny(['\t', '\n', '\r', '"']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text);
            }

            sb.Append("\r\n");
        }

        var result = sb.ToString();
        _clipboard = new ClipboardSource(_sheet, range, rows, cut, result);
        StateHasChanged();
        await Js("writeClipboard", result, HtmlTableFormat.Write(_sheet, range, Culture));
    }

    internal static List<List<string>> ParseTabular(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        var atStart = true;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (ch == '"') quoted = false;
                else field.Append(ch);
                continue;
            }

            switch (ch)
            {
                case '"' when atStart:
                    quoted = true;
                    atStart = false;
                    continue;
                case '\t':
                    row.Add(field.ToString());
                    field.Clear();
                    atStart = true;
                    continue;
                case '\r' or '\n':
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                    atStart = true;
                    continue;
            }

            field.Append(ch);
            atStart = false;
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }

    private sealed record ClipboardSource(Worksheet Sheet, CellRange Range, IReadOnlyList<int> Rows, bool Cut, string Text);
}
