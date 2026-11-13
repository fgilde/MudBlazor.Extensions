using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookCanvas
{
    internal const double DefaultShapeSize = 100;
    internal const long MaxPictureBytes = 50L * 1024 * 1024;
    private const double MinShapeSize = 4;

    /// <summary>The kind of shape the <see cref="NotebookTool.Shape"/> tool draws.</summary>
    [Parameter] public NoteShapeKind ShapeKind { get; set; } = NoteShapeKind.Rectangle;

    /// <summary>Raised when the canvas switches the tool itself, for example back to <see cref="NotebookTool.Select"/> after a shape was drawn.</summary>
    [Parameter] public EventCallback<NotebookTool> ToolChanged { get; set; }

    /// <summary>Raised when pictures were dropped on the page, with their content and the drop position in page coordinates.</summary>
    [Parameter] public EventCallback<NotebookPictureDrop> PictureDropped { get; set; }

    /// <summary>Raised for a right click on the page; <see cref="ContextTarget"/> tells which element and table cell was clicked.</summary>
    [Parameter] public EventCallback<MouseEventArgs> ContextMenu { get; set; }

    /// <summary>The position of the last click on the page in page coordinates, or null.</summary>
    public (double X, double Y)? LastClick { get; private set; }

    internal (string Id, int Row, int Column)? ContextTarget { get; private set; }

    /// <summary>
    /// Called by the browser once a shape was dragged open in the shape tool, with the start and end point in page coordinates. A click without
    /// dragging adds a shape of 100 pixels. Lines and arrows point from the start to the end.
    /// </summary>
    /// <param name="x1">The horizontal start.</param>
    /// <param name="y1">The vertical start.</param>
    /// <param name="x2">The horizontal end.</param>
    /// <param name="y2">The vertical end.</param>
    /// <returns>A task that completes when the shape is added and selected.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnShapeAsync(double x1, double y1, double x2, double y2) => InvokeAsync(async () =>
    {
        if (!Editable || Tool != NotebookTool.Shape || !new[] { x1, y1, x2, y2 }.All(double.IsFinite))
            return;
        (x1, y1, x2, y2) = (Math.Max(0, x1), Math.Max(0, y1), Math.Max(0, x2), Math.Max(0, y2));
        var line = ShapeKind is NoteShapeKind.Line or NoteShapeKind.Arrow;
        if (Math.Abs(x2 - x1) < MinShapeSize && Math.Abs(y2 - y1) < MinShapeSize)
            (x2, y2) = (x1 + DefaultShapeSize, line ? y1 : y1 + DefaultShapeSize);
        var shape = new ShapeElement
        {
            Id = NoteValues.NewId(), Kind = ShapeKind, X = Math.Min(x1, x2), Y = Math.Min(y1, y2), Width = Math.Abs(x2 - x1), Height = Math.Abs(y2 - y1),
            FlipX = line && x2 < x1, FlipY = line && y2 < y1
        };
        await CommitTextAsync();
        Execute(new AddElementCommand(Page, shape));
        await SelectAsync([shape.Id]);
        await ToolChanged.InvokeAsync(NotebookTool.Select);
    });

    /// <summary>Called by the browser for every picture dropped on the page.</summary>
    /// <param name="data">The picture file.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="x">The horizontal drop position in page coordinates.</param>
    /// <param name="y">The vertical drop position in page coordinates.</param>
    /// <returns>A task that completes when the picture is handed to the host.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnPictureDroppedAsync(IJSStreamReference data, string contentType, double x, double y)
    {
        byte[] bytes;
        await using (data)
        {
            if (!Editable || !double.IsFinite(x) || !double.IsFinite(y))
                return;
            await using var stream = await data.OpenReadStreamAsync(MaxPictureBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            bytes = buffer.ToArray();
        }

        await InvokeAsync(() => PictureDropped.InvokeAsync(new NotebookPictureDrop(bytes, contentType, Math.Max(0, x), Math.Max(0, y))));
    }

    /// <summary>Called by the browser for a right click on the page. Selects the clicked element unless it is selected already.</summary>
    /// <param name="id">The id of the clicked element, or null for the free page.</param>
    /// <param name="row">The row of the clicked table cell, or -1.</param>
    /// <param name="column">The column of the clicked table cell, or -1.</param>
    /// <param name="clientX">The horizontal position in the window.</param>
    /// <param name="clientY">The vertical position in the window.</param>
    /// <returns>A task that completes when the menu is requested.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnContextMenuAsync(string id, int row, int column, double clientX, double clientY) => InvokeAsync(async () =>
    {
        if (!Editable)
            return;
        var element = id == null ? null : Find(id);
        ContextTarget = element == null ? null : (element.Id, element is TableElement ? row : -1, element is TableElement ? column : -1);
        if (element != null && !_selection.Contains(element.Id))
            await SelectAsync([element.Id]);
        await ContextMenu.InvokeAsync(new MouseEventArgs { ClientX = clientX, ClientY = clientY });
    });
}

/// <summary>A picture dropped on a <see cref="MudExNotebookCanvas"/>.</summary>
/// <param name="Data">The content of the file.</param>
/// <param name="ContentType">The content type the browser reported.</param>
/// <param name="X">The horizontal drop position in page coordinates.</param>
/// <param name="Y">The vertical drop position in page coordinates.</param>
public sealed record NotebookPictureDrop(byte[] Data, string ContentType, double X, double Y);
