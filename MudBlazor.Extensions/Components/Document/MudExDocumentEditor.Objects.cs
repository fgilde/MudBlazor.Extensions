using System.Globalization;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Document;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    private const long EmuPerTwip = 635;
    private const long MaxPictureEmu = 9525L * 20000;

    private static readonly Border NoBorder = new("nil", 0, "auto");
    private static readonly Border LineBorder = new("single", 0.5, "#000000");
    private static readonly string[] MiniColors = ["#000000", "#C00000", "#0070C0", "#00B050"];

    private MudExColor _shading = "#FFFF00";
    private bool _pictureLock = true;
    private (double X, double Y)? _menu;
    private (double X, double Y)? _mini;

    private readonly record struct ObjectState(bool Table, bool Spans, bool HeaderRow, bool Picture, long Width, long Height);

    private ObjectState ObjectsOf(DocumentRange range)
    {
        var table = DocumentTableContext.At(_document, range);
        var picture = PictureAt(range);
        return new ObjectState(table != null, table?.Spans == true, table?.IsHeader == true, picture != null, picture?.Image.WidthEmu ?? 0, picture?.Image.HeightEmu ?? 0);
    }

    private ObjectState Objects => Format == null ? default : ObjectsOf(Selection);

    private (DocumentPosition At, InlineImage Image)? PictureAt(DocumentRange range)
    {
        if (range.Start == null || range.End == null || range.IsEmpty)
            return null;
        try
        {
            var (start, end) = range.Normalize(_document);
            if (start.BlockId != end.BlockId || end.Offset - start.Offset != 1)
                return null;
            var (paragraph, offset) = _document.Resolve(start);
            return InlineEdit.At(paragraph, offset) is InlineImage image ? (start, image) : null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private Task TableAsync(Func<DocumentTableContext, IDocumentCommand<TextDocument>> command) => RunAsync(range =>
    {
        if (Live(range) && DocumentTableContext.At(_document, range) is { } at && command(at) is { } result)
            _document.Execute(result, range);
    });

    internal Task InsertRowAsync(bool below) => TableAsync(t => new InsertTableRowsCommand(t.Table.Id, below ? t.LastRow + 1 : t.Row, 1));

    internal Task InsertColumnAsync(bool right) => TableAsync(t => new InsertTableColumnsCommand(t.Table.Id, right ? t.LastColumn + 1 : t.Column, 1));

    internal Task DeleteRowsAsync() => TableAsync(t => new DeleteTableRowsCommand(t.Table.Id, t.Row, t.LastRow - t.Row + 1));

    internal Task DeleteColumnsAsync() => TableAsync(t => new DeleteTableColumnsCommand(t.Table.Id, t.Column, t.LastColumn - t.Column + 1));

    internal Task DeleteTableAsync() => TableAsync(t => new DeleteTableCommand(t.Table.Id));

    internal Task MergeCellsAsync() => TableAsync(t => t.Spans ? new MergeTableCellsCommand(t.Table.Id, t.Row, t.Column, t.LastRow, t.LastColumn) : null);

    internal Task SplitCellAsync() => TableAsync(t => new SplitTableCellCommand(t.Table.Id, t.StartRow, t.Column));

    internal Task ToggleHeaderRowAsync() => TableAsync(t => new SetTableHeaderRowsCommand(t.Table.Id, t.IsHeader ? 0 : 1));

    private Task PickShadingAsync(MudExColor color)
    {
        _shading = color;
        return SetShadingAsync(MudExNotebookEditor.Hex(color));
    }

    internal Task SetShadingAsync(string color) =>
        DocumentCss.Color(color ?? "") is { } valid ? TableAsync(t => new SetTableCellFormatCommand(t.Table.Id, t.Cells(), c => c with { Shading = valid }, "Cell shading")) : Task.CompletedTask;

    internal Task SetBordersAsync(string kind) => RunAsync(range =>
    {
        if (!Live(range) || DocumentTableContext.At(_document, range) is not { } at || kind is not ("all" or "none" or "outside"))
            return;
        var (outer, inner) = kind switch { "all" => (LineBorder, LineBorder), "none" => (NoBorder, NoBorder), _ => (LineBorder, NoBorder) };
        var borders = new TableBorders(outer, outer, outer, outer, inner, inner);
        var all = Enumerable.Range(0, at.Table.Rows.Count).SelectMany(r => Tables.Layout(at.Table.Rows[r]).Select(l => (r, l.Column))).ToList();
        using (_document.Undo.BeginTransaction("Borders"))
        {
            _document.Execute(new SetTableFormatCommand(at.Table.Id, f => f with { Borders = borders }, "Borders"), range);
            _document.Execute(new SetTableCellFormatCommand(at.Table.Id, all, c => c with { Borders = null }, "Borders"), range);
        }
    });

    internal async Task OpenTablePropertiesAsync()
    {
        (DocumentRange Range, Table Table) info = default;
        await RunAsync(range => info = Live(range) && DocumentTableContext.At(_document, range) is { } at ? (range, at.Table) : default);
        if (info.Table == null)
            return;
        var reference = await DialogService.ShowExAsync<MudExDocumentTablePropertiesDialog>(TryLocalize("Table Properties"), new DialogParameters
        {
            { nameof(MudExDocumentTablePropertiesDialog.Alignment), info.Table.Format.Alignment },
            { nameof(MudExDocumentTablePropertiesDialog.Widths), info.Table.Grid },
            { nameof(MudExDocumentTablePropertiesDialog.Metric), DocumentUnits.Metric(CultureInfo.CurrentCulture) },
            { nameof(MudExDocumentTablePropertiesDialog.Localizer), Localizer }
        }, DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(info.Range) && result is { Canceled: false, Data: ValueTuple<TableAlignment?, List<int>> data })
            await ApplyTablePropertiesAsync(data.Item1, data.Item2);
        await FocusSurfaceAsync();
    }

    internal Task ApplyTablePropertiesAsync(TableAlignment? alignment, IReadOnlyList<int> widths) => RunAsync(range =>
    {
        if (!Live(range) || DocumentTableContext.At(_document, range) is not { } at)
            return;
        using (_document.Undo.BeginTransaction("Table properties"))
        {
            _document.Execute(new SetTableFormatCommand(at.Table.Id, f => f with { Alignment = alignment }, "Table properties"), range);
            if (widths?.Count == at.Table.Grid.Count && widths.All(w => w > 0))
                _document.Execute(new SetTableColumnWidthsCommand(at.Table.Id, widths), range);
        }
    });

    private static double EmuPerUnit => DocumentUnits.Metric(CultureInfo.CurrentCulture) ? 360000 : 914400;

    private static double ToUnit(long emu) => Math.Round(emu / EmuPerUnit, 2);

    private static double UnitStep => DocumentUnits.Metric(CultureInfo.CurrentCulture) ? 0.1 : 0.05;

    internal Task SetPictureSizeAsync(double? width, double? height) => RunAsync(range =>
    {
        if (!Live(range) || PictureAt(range) is not { } picture)
            return;
        var (w, h) = (picture.Image.WidthEmu, picture.Image.HeightEmu);
        if (width is > 0 and var newWidth && double.IsFinite(newWidth))
        {
            w = Math.Clamp((long)Math.Round(newWidth * EmuPerUnit), 1, MaxPictureEmu);
            h = _pictureLock ? Math.Max(1, (long)Math.Round((double)w * picture.Image.HeightEmu / picture.Image.WidthEmu)) : h;
        }
        else if (height is > 0 and var newHeight && double.IsFinite(newHeight))
        {
            h = Math.Clamp((long)Math.Round(newHeight * EmuPerUnit), 1, MaxPictureEmu);
            w = _pictureLock ? Math.Max(1, (long)Math.Round((double)h * picture.Image.WidthEmu / picture.Image.HeightEmu)) : w;
        }

        if ((w, h) != (picture.Image.WidthEmu, picture.Image.HeightEmu))
            _document.Execute(new ResizeImageCommand(picture.At, Math.Min(w, MaxPictureEmu), Math.Min(h, MaxPictureEmu)), range);
    });

    internal Task ResetPictureSizeAsync() => RunAsync(range =>
    {
        if (!Live(range) || PictureAt(range) is not { } picture || _document.GetMedia(picture.Image.MediaPart) is not { } data)
            return;
        var (w, h) = InsertImageCommand.Size(data, _document.MediaContentType(picture.Image.MediaPart), _document.SectionOf(picture.At.BlockId).Page.TextWidth * EmuPerTwip);
        if ((w, h) != (picture.Image.WidthEmu, picture.Image.HeightEmu))
            _document.Execute(new ResizeImageCommand(picture.At, w, h), range);
    });

    internal async Task OpenAltTextDialogAsync()
    {
        (DocumentRange Range, string Text) info = default;
        await RunAsync(range => info = Live(range) && PictureAt(range) is { } picture ? (range, picture.Image.AltText ?? "") : default);
        if (info.Range.Start == null)
            return;
        var reference = await DialogService.ShowExAsync<MudExNotebookAltTextDialog>(TryLocalize("Alt Text"), new DialogParameters
        {
            { nameof(MudExNotebookAltTextDialog.Text), info.Text },
            { nameof(MudExNotebookAltTextDialog.Localizer), Localizer }
        }, DialogOptionsEx.EditorDialogOptions(DialogOptions));
        var result = await reference.Result;
        if (await ReselectAsync(info.Range) && result is { Canceled: false, Data: string text })
            await SetAltTextAsync(text);
        await FocusSurfaceAsync();
    }

    internal Task SetAltTextAsync(string text) => RunAsync(range =>
    {
        if (Live(range) && PictureAt(range) is { } picture)
            _document.Execute(new SetImageAltTextCommand(picture.At, text), range);
    });

    private Task OnContextMenuAsync((double X, double Y) at)
    {
        (_menu, _mini) = (IsReadOnly ? null : at, null);
        return Task.CompletedTask;
    }

    private Task OnMiniToolbarAsync((double X, double Y)? at)
    {
        _mini = IsReadOnly || _menu != null ? null : at;
        return Task.CompletedTask;
    }

    private void CloseMenu() => _menu = null;

    private async Task MenuAsync(Func<Task> action)
    {
        _menu = null;
        StateHasChanged();
        await action();
        await FocusSurfaceAsync();
    }
}
