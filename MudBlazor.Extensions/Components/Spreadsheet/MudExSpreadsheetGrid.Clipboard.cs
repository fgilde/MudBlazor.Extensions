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
    private const long MaxPasteBytes = 256L * 1024 * 1024;

    private ClipboardSource _clipboard;

    /// <summary>Copies the current range as tab separated text to the clipboard.</summary>
    /// <returns>A task that completes when the text is handed to the browser.</returns>
    public Task CopyAsync() => CopyCoreAsync(false);

    /// <summary>Copies the current range and moves it on the next paste.</summary>
    /// <returns>A task that completes when the text is handed to the browser.</returns>
    public Task CutAsync() => CopyCoreAsync(CanEdit);

    /// <summary>
    /// Pastes text at the top-left cell of the selection. Tab separated text fills a range; each value is parsed like typed input.
    /// Text that was copied from this grid pastes the original cells with formulas, adjusting relative references; cut cells are
    /// moved with their formulas unchanged.
    /// </summary>
    /// <param name="text">The clipboard text.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    public async Task PasteTextAsync(string text)
    {
        if (!CanEdit || string.IsNullOrEmpty(text)) return;
        var origin = _selection.Range.Normalized;
        CellRange target = default;
        if (!await TryWriteAsync(() => target = Paste(text, origin))) return;
        await SelectAsync(new SpreadsheetSelection(target.Start, [target]), target.Start);
        StateHasChanged();
    }

    /// <summary>Called by the browser on paste. The text arrives as a stream, so large clipboards do not hit the message size limit of Blazor Server.</summary>
    /// <param name="data">The clipboard text as UTF-8.</param>
    /// <returns>A task that completes when the cells are written.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task PasteStreamAsync(IJSStreamReference data)
    {
        string text;
        try
        {
            text = await ReadTextAsync(data);
        }
        catch (Exception e)
        {
            await ReportErrorAsync(e);
            return;
        }

        await PasteTextAsync(text);
    }

    internal async Task<string> ReadClipboardAsync()
    {
        if (JsReference is not { } js) return null;
        return await ReadTextAsync(await js.InvokeAsync<IJSStreamReference>("readClipboard"));
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

    private CellRange Paste(string text, CellRange origin)
    {
        CellRange target;
        if (_clipboard is { } clip && clip.Text == text && _workbook.Sheets.Contains(clip.Sheet))
        {
            target = CellRange.FromBounds(origin.Top, origin.Left, Math.Min(CellRef.MaxRows - 1, origin.Top + clip.Range.RowCount - 1),
                Math.Min(CellRef.MaxColumns - 1, origin.Left + clip.Range.ColumnCount - 1));
            var cells = clip.Range.Cells().ToList();
            var dRow = target.Top - clip.Range.Top;
            if (clip.Sheet == _sheet && (dRow > 0 || (dRow == 0 && target.Left > clip.Range.Left))) cells.Reverse();
            using (_workbook.Undo.BeginTransaction(clip.Cut ? "Move" : "Paste"))
            {
                foreach (var s in cells)
                {
                    var t = new CellRef(s.Row - clip.Range.Top + target.Top, s.Column - clip.Range.Left + target.Left);
                    if (target.Contains(t)) CopyCell(clip.Sheet, s, t, !clip.Cut);
                }

                if (clip.Cut)
                    foreach (var s in cells)
                        if (clip.Sheet != _sheet || !target.Contains(s))
                            ClearCell(clip.Sheet, s, true);
            }

            if (clip.Cut) _clipboard = null;
            return target;
        }

        var rows = ParseTabular(text);
        if (rows.Count == 0) return new CellRange(origin.Start, origin.Start);
        var width = rows.Max(r => r.Count);
        target = CellRange.FromBounds(origin.Top, origin.Left, Math.Min(CellRef.MaxRows - 1, origin.Top + rows.Count - 1),
            Math.Min(CellRef.MaxColumns - 1, origin.Left + width - 1));
        using (_workbook.Undo.BeginTransaction("Paste"))
            for (var i = 0; i < rows.Count; i++)
                for (var j = 0; j < rows[i].Count; j++)
                {
                    var t = new CellRef(origin.Top + i, origin.Left + j);
                    if (target.Contains(t)) WriteInput(t, rows[i][j]);
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
        var sb = new StringBuilder();
        for (var r = range.Top; r <= range.Bottom; r++)
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
        _clipboard = new ClipboardSource(_sheet, range, cut, result);
        StateHasChanged();
        await Js("writeClipboard", result);
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

    private sealed record ClipboardSource(Worksheet Sheet, CellRange Range, bool Cut, string Text);
}
