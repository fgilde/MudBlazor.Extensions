using System.ComponentModel;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookEditor
{
    internal const double MaxPictureWidth = 600;
    private const double DefaultInsertOffset = 40;

    private static readonly Dictionary<string, string> PictureTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".gif"] = "image/gif", [".webp"] = "image/webp", [".bmp"] = "image/bmp",
        [".svg"] = "image/svg+xml"
    };

    private static readonly (string Id, string Text, string Icon, NoteShapeKind Kind)[] ShapeKinds =
    [
        ("nb-shape-line", "Line", Icons.Material.Filled.HorizontalRule, NoteShapeKind.Line),
        ("nb-shape-arrow", "Arrow", Icons.Material.Filled.ArrowRightAlt, NoteShapeKind.Arrow),
        ("nb-shape-rectangle", "Rectangle", Icons.Material.Outlined.Rectangle, NoteShapeKind.Rectangle),
        ("nb-shape-ellipse", "Ellipse", Icons.Material.Outlined.Circle, NoteShapeKind.Ellipse),
        ("nb-shape-triangle", "Triangle", Icons.Material.Outlined.ChangeHistory, NoteShapeKind.Triangle),
        ("nb-shape-diamond", "Diamond", Icons.Material.Outlined.Diamond, NoteShapeKind.Diamond)
    ];

    private readonly string _pictureInput = $"mud-ex-notebook-picture-{Guid.NewGuid():N}";
    private NoteShapeKind _shapeKind = NoteShapeKind.Rectangle;
    private MudMenu _contextMenu;
    private IReadOnlyList<string> _pendingSelection;
    private bool _searchVisible;
    private bool _searchNotebook;
    private string _searchQuery;

    private static string PictureAccept => string.Join(",", PictureTypes.Keys);

    private IReadOnlyList<NoteElement> SelectedElements => _canvas?.Selection is { Count: > 0 } ids && _page != null
        ? _page.Elements.Where(e => ids.Contains(e.Id)).ToList()
        : [];

    private IReadOnlyList<string> StylableIds => SelectedElements.Where(e => e is ShapeElement or InkElement).Select(e => e.Id).ToList();

    private ShapeElement SelectedShape => SelectedElements.OfType<ShapeElement>().FirstOrDefault();

    private ImageElement SelectedImage => SelectedElements is [ImageElement image] ? image : null;

    private (TableElement Table, int Row, int Column)? MenuCell =>
        _canvas?.ContextTarget is ({ } id, var row, var column) && row >= 0 && _page?.Elements.FirstOrDefault(e => e.Id == id) is TableElement table
        && row < table.Rows.Count && column >= 0 && column < table.ColumnWidths.Count
            ? (table, row, column)
            : null;

    private (double X, double Y) InsertPoint => _canvas?.LastClick ?? (DefaultInsertOffset, DefaultInsertOffset);

    private async Task ChooseShapeAsync(NoteShapeKind kind)
    {
        _shapeKind = kind;
        await SetToolAsync(NotebookTool.Shape);
    }

    private Task SetShapeOutlineAsync(MudExColor color) => Hex(color) is { } hex ? StyleAsync(hex, null, null) : Task.CompletedTask;

    private Task SetShapeFillAsync(MudExColor color) => Hex(color) is { } hex ? StyleAsync(null, null, hex) : Task.CompletedTask;

    private Task SetShapeWeightAsync(double width) => double.IsFinite(width) && width >= 0 ? StyleAsync(null, Math.Min(width, 100), null) : Task.CompletedTask;

    private Task StyleAsync(string stroke, double? width, string fill)
    {
        var ids = StylableIds;
        return ids.Count == 0 || (width == 0 && SelectedElements.Any(e => e is InkElement && ids.Contains(e.Id)))
            ? Task.CompletedTask
            : RunCommandAsync(new SetShapeStyleCommand(_page, ids, stroke, width, fill));
    }

    private Task PickPictureAsync() =>
        !CanEditPage ? Task.CompletedTask : JsRuntime.InvokeVoidAsync("MudExEventHelper.clickElementById", _pictureInput).AsTask();

    private async Task OnPictureChosenAsync(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            await using var stream = file.OpenReadStream(MudExNotebookCanvas.MaxPictureBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            var (x, y) = InsertPoint;
            await InsertPictureAsync(buffer.ToArray(), PictureTypes.GetValueOrDefault(Path.GetExtension(file.Name)) ?? file.ContentType, x, y);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
    }

    private Task OnPictureDroppedAsync(NotebookPictureDrop drop) => InsertPictureAsync(drop.Data, drop.ContentType, drop.X, drop.Y);

    internal async Task InsertPictureAsync(byte[] data, string contentType, double x, double y)
    {
        if (!CanEditPage || data is not { Length: > 0 })
            return;
        if (!PictureTypes.ContainsValue(contentType ?? ""))
        {
            await ShowErrorAsync(TryLocalize("The picture format is not supported."));
            return;
        }

        await CommitTextAsync();
        if (_js.Js is { } js && _images is { } images && SlideImageUrls.RasterTypes.Contains(contentType))
        {
            try
            {
                if (await js.InvokeAsync<string>("register", data, contentType) is { } url)
                    images.Register(Office.Notebook.Notebook.MediaIdOf(data), url);
            }
            catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        var command = new AddImageCommand(_page, data, contentType, x, y, MaxPictureWidth);
        await RunCommandAsync(command);
        if (command.Image != null && _canvas != null)
            await _canvas.SelectAsync([command.Image.Id]);
    }

    internal async Task InsertTableAsync((int Rows, int Columns) size)
    {
        if (!CanEditPage)
            return;
        var (x, y) = InsertPoint;
        var command = new AddTableCommand(_page, Math.Clamp(size.Rows, 1, 8), Math.Clamp(size.Columns, 1, 10), x, y);
        await RunCommandAsync(command);
        if (command.Table != null && _canvas != null)
            await _canvas.EditCellAsync(command.Table.Id, 0, 0);
    }

    private Task TableCommandAsync(Func<TableElement, int, int, IDocumentCommand<Office.Notebook.Notebook>> create) =>
        MenuCell is var (table, row, column) ? RunCommandAsync(create(table, row, column)) : Task.CompletedTask;

    private Task InsertRowAsync(bool below) => TableCommandAsync((t, r, _) => new InsertTableRowsCommand(_page, t.Id, below ? r + 1 : r));

    private Task InsertColumnAsync(bool right) => TableCommandAsync((t, _, c) => new InsertTableColumnsCommand(_page, t.Id, right ? c + 1 : c));

    private Task DeleteRowAsync() => TableCommandAsync((t, r, _) => t.Rows.Count == 1 ? new DeleteElementsCommand(_page, [t.Id]) : new DeleteTableRowsCommand(_page, t.Id, r));

    private Task DeleteColumnAsync() =>
        TableCommandAsync((t, _, c) => t.ColumnWidths.Count == 1 ? new DeleteElementsCommand(_page, [t.Id]) : new DeleteTableColumnsCommand(_page, t.Id, c));

    private Task DeleteTableAsync() => TableCommandAsync((t, _, _) => new DeleteElementsCommand(_page, [t.Id]));

    private Task ToggleHeaderRowAsync() => TableCommandAsync((t, _, _) => new SetTableHeaderRowCommand(_page, t.Id, !t.HeaderRow));

    private Task OpenContextMenuAsync(MouseEventArgs e) => _contextMenu == null ? Task.CompletedTask : _contextMenu.OpenMenuAsync(e);

    private async Task EditAltTextAsync()
    {
        if (SelectedImage is not { } image)
            return;
        var page = _page;
        var reference = await DialogService.ShowAsync<MudExNotebookAltTextDialog>(TryLocalize("Alt Text"), new DialogParameters
        {
            { nameof(MudExNotebookAltTextDialog.Text), image.AltText },
            { nameof(MudExNotebookAltTextDialog.Localizer), Localizer }
        }, new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
        if (await reference.Result is { Canceled: false, Data: string text } && ReferenceEquals(page, _page))
            await RunCommandAsync(new SetImageAltTextCommand(page, image.Id, text));
    }

    private async Task OpenSearchAsync(bool wholeNotebook)
    {
        await CommitTextAsync();
        await CommitTitleAsync();
        _searchNotebook = wholeNotebook;
        _searchVisible = true;
    }

    private async Task SelectSearchResultAsync(NotebookSearchResult result)
    {
        IReadOnlyList<string> ids = result.ElementId == null ? [] : [result.ElementId];
        if (await GoAsync(result.Section, result.Page))
            _pendingSelection = ids;
        else if (ReferenceEquals(result.Page, _page) && _canvas != null)
        {
            await _canvas.SelectAsync(ids);
            await _canvas.RevealAsync(result.ElementId);
        }
    }

    private async Task ApplyPendingSelectionAsync()
    {
        if (_pendingSelection is not { } ids || _canvas == null)
            return;
        _pendingSelection = null;
        await _canvas.SelectAsync(ids);
        await _canvas.RevealAsync(ids.FirstOrDefault());
        StateHasChanged();
    }

    private Task PrintAsync() => _js.Js is { } js ? js.InvokeVoidAsync("print").AsTask() : Task.CompletedTask;

    /// <summary>Called by the browser for Ctrl+mouse wheel over the page.</summary>
    /// <param name="factor">The factor to multiply the zoom with.</param>
    /// <returns>A task that completes when the page is shown at the new zoom.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnZoomWheel(double factor) => InvokeAsync(() =>
    {
        if (!double.IsFinite(factor) || factor <= 0)
            return;
        SetZoom(factor > 1 ? ZoomLevels.FirstOrDefault(z => z > _zoom, _zoom) : ZoomLevels.LastOrDefault(z => z < _zoom, _zoom));
        StateHasChanged();
    });
}
