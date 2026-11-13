using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideCanvas
{
    private const string TextBoxInsert = "text-box";
    private const string Accent = "scheme:accent1";
    private const long ShapeSize = 914400;

    private string _insert;
    private (Slide Slide, uint[] Ids)? _pendingSelection;

    /// <summary>Raised with the new zoom factor when the user turns the mouse wheel with Ctrl pressed.</summary>
    [Parameter] public EventCallback<double> ZoomChanged { get; set; }

    /// <summary>
    /// Called by the browser when the user clicked or dragged on the slide to insert the chosen shape or a text box. A click inserts the default size.
    /// </summary>
    /// <param name="x">The horizontal start position in slide coordinates (EMU).</param>
    /// <param name="y">The vertical start position in slide coordinates (EMU).</param>
    /// <param name="width">The horizontal drag distance in EMU, negative to the left; zero for a click.</param>
    /// <param name="height">The vertical drag distance in EMU, negative upwards; zero for a click.</param>
    /// <returns>A task that completes when the shape is inserted and selected.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnInsertAsync(double x, double y, double width, double height) => InvokeAsync(async () =>
    {
        if (_insert is not { } insert || ReadOnly || Slide == null)
            return;
        _insert = null;
        CommitText();
        if (insert == TextBoxInsert)
        {
            await InsertTextBoxAsync(Math.Min(x, x + width), Math.Min(y, y + height), Math.Abs(width));
            return;
        }

        var line = MudExSlideShapeGallery.IsLine(insert);
        if (Math.Abs(width) < 1 && Math.Abs(height) < 1)
            (width, height) = (ShapeSize, line ? 0 : ShapeSize);
        var transform = new ShapeTransform((long)Math.Round(Math.Min(x, x + width)), (long)Math.Round(Math.Min(y, y + height)),
            Math.Max(line ? 0 : 1, (long)Math.Round(Math.Abs(width))), Math.Max(line ? 0 : 1, (long)Math.Round(Math.Abs(height))), 0, line && width < 0, line && height < 0);
        var shape = line
            ? AutoShape.Create(insert, transform, new ShapeFill(ShapeFillKind.None), new ShapeLine(Accent, 12700))
            : AutoShape.CreateStyled(insert, transform);
        Presentation.Execute(new InsertShapeCommand(Slide, shape));
        shape.Name = $"{MudExSlideShapeGallery.NameOf(insert)} {shape.Id - 1}";
        await SelectAsync([shape.Id]);
    });

    /// <summary>Called by the browser when the user zoomed with Ctrl and the mouse wheel.</summary>
    /// <param name="zoom">The new zoom factor.</param>
    /// <returns>A task that completes when the zoom is handed to the host.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnWheelZoomAsync(double zoom) => InvokeAsync(() => ZoomChanged.InvokeAsync(zoom));

    internal Task InsertShapeAsync(string preset)
    {
        if (ReadOnly || Slide == null || !PresetGeometry.IsSupported(preset))
            return Task.CompletedTask;
        _insert = preset;
        return CommitTextAsync();
    }

    internal Task SelectAsync(Slide slide, IEnumerable<uint> ids)
    {
        if (ReferenceEquals(slide, Slide))
            return SelectAsync(ids);
        _pendingSelection = (slide, ids.ToArray());
        return Task.CompletedTask;
    }
}
