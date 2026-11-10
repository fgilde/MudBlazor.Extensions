using System.Globalization;
using System.Text;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Spreadsheet;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

public partial class MudExSpreadsheetEditor
{
    private static readonly string[] Fonts =
        ["Aptos", "Arial", "Calibri", "Cambria", "Candara", "Consolas", "Courier New", "Georgia", "Segoe UI", "Tahoma", "Times New Roman", "Trebuchet MS", "Verdana"];

    private static readonly string[] NumberFormatNames =
        ["General", "Number", "Currency", "Accounting", "Short Date", "Long Date", "Time", "Percentage", "Fraction", "Scientific", "Text"];

    private static readonly double[] ZoomLevels = [0.25, 0.5, 0.75, 1, 1.25, 1.5, 2, 3, 4];

    private static readonly IReadOnlyList<MudExColor> Palette =
    [
        "#000000", "#FFFFFF", "#E7E6E6", "#44546A", "#4472C4", "#ED7D31", "#A5A5A5", "#FFC000", "#5B9BD5", "#70AD47",
        "#C00000", "#FF0000", "#FFFF00", "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0", "#217346"
    ];

    private static readonly (string Name, string Function)[] AutoSumFunctions =
        [("Sum", "SUM"), ("Average", "AVERAGE"), ("Count Numbers", "COUNT"), ("Max", "MAX"), ("Min", "MIN")];

    private static readonly (string Category, FunctionInfo[] Functions)[] FunctionCategories = FunctionRegistry.Default.Functions
        .Select(f => f.Info)
        .GroupBy(i => i.Category)
        .OrderBy(g => g.Key, StringComparer.Ordinal)
        .Select(g => (g.Key == "Math" ? "Math & Trig" : g.Key, g.OrderBy(i => i.Name, StringComparer.Ordinal).ToArray()))
        .ToArray();

    private MudExColor _fontColor = "#C00000";
    private MudExColor _fillColor = "#FFFF00";

    private enum BorderPreset
    {
        Bottom,
        Top,
        Left,
        Right,
        None,
        All,
        Outside,
        ThickOutside
    }

    private bool CanEdit => !IsReadOnly && ActiveSheet != null;

    private CellRange ActiveRange => CurrentSelection.Range.Normalized;

    private string NumberFormatName => NumberFormatNames.FirstOrDefault(n => NumberFormatCode(n) == ActiveStyle.NumberFormat);

    private Task FocusGridAsync() => _grid?.FocusAsync() ?? Task.CompletedTask;

    private async Task RunAsync(Action action)
    {
        if (!CanEdit || !await CommitPendingEditAsync())
            return;
        try
        {
            action();
        }
        catch (Exception e)
        {
            await ShowErrorAsync(e.Message);
        }

        await FocusGridAsync();
    }

    private async Task<bool> CommitPendingEditAsync()
    {
        if (_grid?.IsEditing != true)
            return true;
        await _grid.CommitEditAsync();
        return !_grid.IsEditing;
    }

    private Task OnGridErrorAsync(Exception e) => ShowErrorAsync(e.Message);

    private async Task SaveFromKeyAsync()
    {
        if (!IsReadOnly)
            await SaveAsync();
    }

    private Task RunCommandAsync(IWorkbookCommand command) => RunAsync(() => _workbook.Execute(command));

    private Task ApplyStyleAsync(Func<CellStyle, CellStyle> change, string description) => RunAsync(() => _grid?.ApplyStyle(change, description));

    private Task SetFontAsync(Func<CellFont, CellFont> change, string description) => ApplyStyleAsync(s => s with { Font = change(s.Font) }, description);

    private Task SetBoldAsync(bool value) => SetFontAsync(f => f with { Bold = value }, "Bold");

    private Task SetItalicAsync(bool value) => SetFontAsync(f => f with { Italic = value }, "Italic");

    private Task SetUnderlineAsync(bool value) => SetFontAsync(f => f with { Underline = value }, "Underline");

    private Task SetStrikethroughAsync(bool value) => SetFontAsync(f => f with { Strikethrough = value }, "Strikethrough");

    private Task SetFontNameAsync(string name) => string.IsNullOrWhiteSpace(name) ? Task.CompletedTask : SetFontAsync(f => f with { Name = name.Trim() }, "Font");

    private Task SetFontSizeAsync(double size) => size > 0 ? SetFontAsync(f => f with { Size = size }, "Font size") : Task.CompletedTask;

    private Task SetFontColorAsync(MudExColor color)
    {
        _fontColor = color;
        return SetFontAsync(f => f with { Color = Hex(color) }, "Font color");
    }

    private Task SetFillColorAsync(MudExColor color)
    {
        _fillColor = color;
        return ApplyStyleAsync(s => s with { Fill = new CellFill { Color = Hex(color) } }, "Fill color");
    }

    private static string Hex(MudExColor color) => color.IsSet() ? color.ToMudColor()?.ToString(MudColorOutputFormats.Hex).ToUpperInvariant() : null;

    private Task SetHorizontalAsync(CellHorizontalAlignment value)
    {
        var target = ActiveStyle.Alignment.Horizontal == value ? CellHorizontalAlignment.General : value;
        return ApplyStyleAsync(s => s with { Alignment = s.Alignment with { Horizontal = target } }, "Alignment");
    }

    private Task SetVerticalAsync(CellVerticalAlignment value)
        => ApplyStyleAsync(s => s with { Alignment = s.Alignment with { Vertical = value } }, "Alignment");

    private Task SetWrapAsync(bool value) => ApplyStyleAsync(s => s with { Alignment = s.Alignment with { WrapText = value } }, "Wrap text");

    private Task SetNumberFormatAsync(string code) => code == null ? Task.CompletedTask : ApplyStyleAsync(s => s with { NumberFormat = code }, "Number format");

    private Task ChangeDecimalsAsync(int delta)
    {
        var code = ActiveStyle.NumberFormat;
        var sample = ActiveSheet?[CurrentSelection.Active]?.Value ?? default;
        return SetNumberFormatAsync(ChangeDecimals(code, delta, sample));
    }

    internal static string NumberFormatCode(string name)
    {
        var culture = CultureInfo.CurrentCulture;
        var symbol = "\"" + culture.NumberFormat.CurrencySymbol + "\"";
        var before = culture.NumberFormat.CurrencyPositivePattern is 0 or 2;
        return name switch
        {
            "General" => NumberFormat.General,
            "Number" => "0.00",
            "Currency" => before ? symbol + "#,##0.00" : "#,##0.00 " + symbol,
            "Accounting" => before
                ? $"_({symbol}* #,##0.00_);_({symbol}* \\(#,##0.00\\);_({symbol}* \"-\"??_);_(@_)"
                : $"_-* #,##0.00 {symbol}_-;-* #,##0.00 {symbol}_-;_-* \"-\"?? {symbol}_-;_-@_-",
            "Short Date" => BuiltInNumberFormats.Get(14, culture),
            "Long Date" => culture.DateTimeFormat.LongDatePattern.Replace('M', 'm').Replace('\'', '"'),
            "Time" => BuiltInNumberFormats.ShortTime(culture, true, culture.DateTimeFormat.LongTimePattern.Contains('t')),
            "Percentage" => "0.00%",
            "Fraction" => "# ?/?",
            "Scientific" => "0.00E+00",
            "Text" => "@",
            _ => null
        };
    }

    internal static string ChangeDecimals(string code, int delta, CellValue sample)
    {
        if (string.IsNullOrEmpty(code) || string.Equals(code, NumberFormat.General, StringComparison.OrdinalIgnoreCase))
        {
            var text = sample.Kind == CellValueKind.Number ? NumberFormat.GeneralText(sample.NumberValue, CultureInfo.InvariantCulture) : "0";
            var dot = text.IndexOf('.');
            var exponent = text.IndexOfAny(['E', 'e']);
            var decimals = Math.Max(0, (dot < 0 ? 0 : (exponent < 0 ? text.Length : exponent) - dot - 1) + delta);
            return decimals == 0 ? "0" : "0." + new string('0', decimals);
        }

        var result = new StringBuilder();
        var start = 0;
        var quoted = false;
        for (var i = 0; i <= code.Length; i++)
        {
            if (i < code.Length && code[i] == '"') quoted = !quoted;
            if (i < code.Length && (quoted || code[i] != ';')) continue;
            result.Append(ChangeSectionDecimals(code[start..i], delta));
            if (i < code.Length) result.Append(';');
            start = i + 1;
        }

        return result.ToString();
    }

    private static string ChangeSectionDecimals(string section, int delta)
    {
        int last = -1, dot = -1;
        var quoted = false;
        for (var i = 0; i < section.Length; i++)
        {
            var c = section[i];
            if (c == '"') quoted = !quoted;
            if (quoted || c == '"') continue;
            if (c is '\\' or '_' or '*') i++;
            else if (c == '[') i = Math.Max(i, section.IndexOf(']', i));
            else if (c is '0' or '#' or '?') last = i;
            else if (c == '.' && dot < 0) dot = i;
            else if (c is 'E' or 'e') break;
            else if (c == '/') return section;
        }

        if (last < 0) return section;
        var hasDecimals = dot >= 0 && dot < last;
        if (delta > 0) return section.Insert(last + 1, hasDecimals ? "0" : ".0");
        if (!hasDecimals) return section;
        return last - dot == 1 ? section.Remove(dot, 2) : section.Remove(last, 1);
    }

    private Task SetBordersAsync(BorderPreset preset) => RunAsync(() =>
    {
        var sheet = ActiveSheet;
        var thin = new CellBorderSide { Style = CellBorderStyle.Thin };
        var side = preset == BorderPreset.ThickOutside ? new CellBorderSide { Style = CellBorderStyle.Thick } : thin;
        using (_workbook.Undo.BeginTransaction("Borders"))
        {
            foreach (var r in CurrentSelection.Ranges.Select(x => x.Normalized))
            {
                void Edge(CellRange range, Func<CellBorder, CellBorder> change) =>
                    _workbook.Execute(new SetStyleCommand(sheet, range, s => s with { Border = change(s.Border) }));

                var top = CellRange.FromBounds(r.Top, r.Left, r.Top, r.Right);
                var bottom = CellRange.FromBounds(r.Bottom, r.Left, r.Bottom, r.Right);
                var left = CellRange.FromBounds(r.Top, r.Left, r.Bottom, r.Left);
                var right = CellRange.FromBounds(r.Top, r.Right, r.Bottom, r.Right);
                var outside = preset is BorderPreset.Outside or BorderPreset.ThickOutside;
                if (preset == BorderPreset.None) Edge(r, _ => new CellBorder());
                if (preset == BorderPreset.All) Edge(r, _ => new CellBorder { Left = thin, Right = thin, Top = thin, Bottom = thin });
                if (outside || preset == BorderPreset.Top) Edge(top, b => b with { Top = side });
                if (outside || preset == BorderPreset.Bottom) Edge(bottom, b => b with { Bottom = side });
                if (outside || preset == BorderPreset.Left) Edge(left, b => b with { Left = side });
                if (outside || preset == BorderPreset.Right) Edge(right, b => b with { Right = side });
            }
        }
    });

    private Task MergeAndCenterAsync()
    {
        var range = ActiveRange;
        if (ActiveSheet?.Merges.Contains(range) == true)
            return UnmergeAsync();
        return RunAsync(() =>
        {
            using (_workbook.Undo.BeginTransaction("Merge & Center"))
            {
                _workbook.Execute(new MergeCommand(ActiveSheet, range));
                _workbook.Execute(new SetStyleCommand(ActiveSheet, range, s => s with { Alignment = s.Alignment with { Horizontal = CellHorizontalAlignment.Center } }));
            }
        });
    }

    private Task MergeAcrossAsync() => RunAsync(() =>
    {
        var r = ActiveRange;
        if (r.ColumnCount < 2) return;
        using (_workbook.Undo.BeginTransaction("Merge Across"))
            for (var row = r.Top; row <= r.Bottom; row++)
                _workbook.Execute(new MergeCommand(ActiveSheet, CellRange.FromBounds(row, r.Left, row, r.Right)));
    });

    private Task MergeCellsAsync() => ActiveRange.IsSingleCell ? Task.CompletedTask : RunCommandAsync(new MergeCommand(ActiveSheet, ActiveRange));

    private Task UnmergeAsync() => RunCommandAsync(new UnmergeCommand(ActiveSheet, ActiveRange));

    internal Task InsertRowsAsync() => RunCommandAsync(new InsertRowsCommand(ActiveSheet, ActiveRange.Top, ActiveRange.RowCount));

    internal Task InsertColumnsAsync() => RunCommandAsync(new InsertColumnsCommand(ActiveSheet, ActiveRange.Left, ActiveRange.ColumnCount));

    internal Task DeleteRowsAsync() => RunCommandAsync(new DeleteRowsCommand(ActiveSheet, ActiveRange.Top, ActiveRange.RowCount));

    internal Task DeleteColumnsAsync() => RunCommandAsync(new DeleteColumnsCommand(ActiveSheet, ActiveRange.Left, ActiveRange.ColumnCount));

    private Task InsertSheetAsync() => RunAsync(() =>
    {
        var command = new AddSheetCommand(MudExSpreadsheetSheetTabs.NextSheetName(_workbook), _sheetIndex);
        _workbook.Execute(command);
        SelectSheet(_workbook.Sheets.ToList().IndexOf(command.Sheet));
    });

    private Task DeleteSheetAsync() => RunCommandAsync(new DeleteSheetCommand(ActiveSheet));

    private Task HideSheetAsync() => RunCommandAsync(new SetSheetHiddenCommand(ActiveSheet, true));

    private Task UnhideSheetAsync(Worksheet sheet) => RunAsync(() =>
    {
        _workbook.Execute(new SetSheetHiddenCommand(sheet, false));
        SelectSheet(_workbook.Sheets.ToList().IndexOf(sheet));
    });

    private Task SetTabColorAsync(string color) => RunCommandAsync(new SetTabColorCommand(ActiveSheet, color));

    private void SelectSheet(int index)
    {
        _sheetIndex = index;
        _activeSheet = _workbook.Sheets[index];
    }

    internal Task SetRowsHiddenAsync(bool hidden) => RunCommandAsync(new SetHiddenCommand(ActiveSheet, Axis.Row, ActiveRange.Top, ActiveRange.RowCount, hidden));

    internal Task SetColumnsHiddenAsync(bool hidden) => RunCommandAsync(new SetHiddenCommand(ActiveSheet, Axis.Column, ActiveRange.Left, ActiveRange.ColumnCount, hidden));

    private Task AutoFitRowsAsync() => RunCommandAsync(new ResizeCommand(ActiveSheet, Axis.Row, ActiveRange.Top, ActiveRange.RowCount, null));

    private async Task AutoFitColumnsAsync()
    {
        if (CanEdit && _grid != null)
            await _grid.AutoFitColumnsAsync();
        await FocusGridAsync();
    }

    internal Task RowHeightAsync() => SizeAsync(Axis.Row, ActiveRange.Top, ActiveRange.RowCount, "Row Height");

    internal Task ColumnWidthAsync() => SizeAsync(Axis.Column, ActiveRange.Left, ActiveRange.ColumnCount, "Column Width");

    private Task SizeAsync(Axis axis, int index, int count, string title) => !CanEdit
        ? Task.CompletedTask
        : ShowCommandDialogAsync<MudExSpreadsheetSizeDialog>(title, new DialogParameters
        {
            { nameof(MudExSpreadsheetSizeDialog.Sheet), ActiveSheet },
            { nameof(MudExSpreadsheetSizeDialog.Axis), axis },
            { nameof(MudExSpreadsheetSizeDialog.Index), index },
            { nameof(MudExSpreadsheetSizeDialog.Count), count }
        });

    private async Task RenameSheetAsync()
    {
        if (!CanEdit) return;
        var sheet = ActiveSheet;
        var name = await DialogService.PromptAsync(TryLocalize("Rename Sheet"), TryLocalize("Sheet name:"), sheet.Name, TryLocalize("OK"), TryLocalize("Cancel"),
            canConfirm: s => !string.IsNullOrWhiteSpace(s));
        if (name != null && name != sheet.Name)
            await RunCommandAsync(new RenameSheetCommand(sheet, name.Trim()));
    }

    private async Task AutoSumAsync(string function)
    {
        if (CanEdit && _grid != null)
            await _grid.AutoSumAsync(function);
    }

    private async Task InsertFunctionAsync(string name)
    {
        if (CanEdit && _grid != null)
            await _grid.BeginEditAsync("=" + name + "(");
    }

    private Task FillDownAsync() => RunAsync(() => _grid?.FillDown());

    private Task FillRightAsync() => RunAsync(() => _grid?.FillRight());

    internal Task ClearAsync(bool contents, bool formats) => RunAsync(() =>
    {
        using (_workbook.Undo.BeginTransaction(contents && formats ? "Clear All" : contents ? "Clear Contents" : "Clear Formats"))
            foreach (var r in CurrentSelection.Ranges.SelectMany(x => _grid?.VisibleParts(x) ?? [x.Normalized]))
                _workbook.Execute(contents
                    ? new ClearCommand(ActiveSheet, r, formats)
                    : new SetStyleCommand(ActiveSheet, r, CellStyle.Default));
    });

    internal async Task CopyAsync()
    {
        if (_grid != null)
            await _grid.CopyAsync();
    }

    internal async Task CutAsync()
    {
        if (CanEdit && _grid != null)
            await _grid.CutAsync();
    }

    internal async Task PasteAsync(bool valuesOnly)
    {
        if (!CanEdit || _grid == null) return;
        string text, html = null;
        try
        {
            text = await _grid.ReadClipboardAsync();
            if (!valuesOnly)
                html = await _grid.ReadClipboardAsync("text/html");
        }
        catch (JSException)
        {
            await ShowErrorAsync(TryLocalize("The browser does not allow reading the clipboard here. Use Ctrl+V to paste."));
            return;
        }

        if (valuesOnly)
            await _grid.PasteValuesAsync(text);
        else
            await _grid.PasteAsync(text, html);
        await FocusGridAsync();
    }

    private async Task UndoAsync(bool redo)
    {
        if (!CanEdit) return;
        if (_grid?.IsEditing == true)
            await _grid.CancelEditAsync();
        else if (redo ? _workbook.Undo.CanRedo : _workbook.Undo.CanUndo)
            await RunAsync(redo ? _workbook.Undo.Redo : _workbook.Undo.Undo);
    }

    private async Task DefineNameAsync()
    {
        if (!CanEdit) return;
        var name = await DialogService.PromptAsync(TryLocalize("New Name"), TryLocalize("Name:"), string.Empty, TryLocalize("OK"), TryLocalize("Cancel"),
            canConfirm: s => !string.IsNullOrWhiteSpace(s) && !s.Trim().Contains(' '));
        if (name == null) return;
        var r = ActiveRange;
        var target = new CellRange(r.Start with { RowAbsolute = true, ColumnAbsolute = true }, r.End with { RowAbsolute = true, ColumnAbsolute = true });
        await RunCommandAsync(new DefineNameCommand(name.Trim(), Office.Spreadsheet.Workbook.QuoteSheetName(ActiveSheet.Name) + "!" + target.ToA1()));
    }

    private Task CalculateNowAsync() => RunAsync(() => _engine?.RecalculateAll());

    private Task FreezeAsync(CellRef? at) => RunCommandAsync(new FreezeCommand(ActiveSheet, at is { Row: 0, Column: 0 } ? null : at));

    private void ZoomToSelection()
    {
        var sheet = ActiveSheet;
        if (sheet == null || _grid == null) return;
        var r = ActiveRange;
        double width = 0, height = 0;
        for (var c = r.Left; c <= Math.Min(r.Right, r.Left + 1000); c++)
            width += sheet.IsColumnHidden(c) ? 0 : sheet.ColumnWidth(c);
        for (var row = r.Top; row <= Math.Min(r.Bottom, r.Top + 10000); row++)
            height += sheet.IsRowHidden(row) ? 0 : sheet.RowHeight(row);
        if (width <= 0 || height <= 0) return;
        OnZoomChanged(Math.Min((_grid.ViewportWidth - 50) / width, (_grid.ViewportHeight - 20) / height));
    }

    internal async Task FormatCellsAsync()
    {
        if (!CanEdit) return;
        var before = ActiveStyle;
        var parameters = new DialogParameters { { nameof(MudExSpreadsheetFormatCellsDialog.CellStyle), before }, { nameof(MudExSpreadsheetFormatCellsDialog.Localizer), Localizer } };
        var reference = await DialogService.ShowAsync<MudExSpreadsheetFormatCellsDialog>(TryLocalize("Format Cells"), parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
        var result = await reference.Result;
        if (result is { Canceled: false, Data: CellStyle after })
            await ApplyStyleAsync(s => s with
            {
                NumberFormat = after.NumberFormat != before.NumberFormat ? after.NumberFormat : s.NumberFormat,
                Alignment = after.Alignment != before.Alignment ? after.Alignment : s.Alignment,
                Font = after.Font != before.Font ? after.Font : s.Font,
                Border = after.Border != before.Border ? after.Border : s.Border,
                Fill = after.Fill != before.Fill ? after.Fill : s.Fill
            }, "Format cells");
        else
            await FocusGridAsync();
    }
}
