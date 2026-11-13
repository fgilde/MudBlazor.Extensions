using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

public partial class MudExNotebookCanvas
{
    private const string DefaultInkColor = "#000000";
    private const double DefaultInkWidth = 2;

    /// <summary>The colour of new strokes as <c>#RRGGBB</c>, used by <see cref="NotebookTool.Pen"/> and <see cref="NotebookTool.Highlighter"/>.</summary>
    [Parameter] public string InkColor { get; set; } = DefaultInkColor;

    /// <summary>The width of new strokes in pixels.</summary>
    [Parameter] public double InkWidth { get; set; } = DefaultInkWidth;

    private string EffectiveInkColor => NoteValues.IsColor(InkColor) ? InkColor : DefaultInkColor;

    private double EffectiveInkWidth => double.IsFinite(InkWidth) && InkWidth > 0 ? InkWidth : DefaultInkWidth;

    private bool IsDrawing => Tool is NotebookTool.Pen or NotebookTool.Highlighter;

    /// <summary>
    /// Called by the browser once a stroke of the pen or highlighter ends. The points are simplified and added as one stroke in one undo step; a single
    /// point is a dot.
    /// </summary>
    /// <param name="points">The points in page coordinates.</param>
    /// <returns>A task that completes when the stroke is added.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnStrokeAsync(InkPointDto[] points) => InvokeAsync(() =>
    {
        if (!Editable || !IsDrawing || points == null)
            return;
        var list = points.Where(p => p != null && double.IsFinite(p.X) && double.IsFinite(p.Y))
            .Select(p => new InkPoint(Math.Max(0, p.X), Math.Max(0, p.Y), double.IsFinite(p.Pressure) ? Math.Clamp(p.Pressure, 0, 1) : 0.5))
            .ToList();
        if (list.Count > 0)
            Execute(new AddStrokeCommand(Page, InkGeometry.Simplify(list), EffectiveInkColor, EffectiveInkWidth,
                Tool == NotebookTool.Highlighter ? InkTool.Highlighter : InkTool.Pen));
    });

    /// <summary>Called by the browser once an eraser gesture ends, with every stroke it touched. They are deleted in one undo step.</summary>
    /// <param name="ids">The ids of the touched strokes; other ids are ignored.</param>
    /// <returns>A task that completes when the strokes are deleted.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnEraseAsync(string[] ids) => InvokeAsync(() =>
    {
        if (!Editable || ids == null)
            return;
        var strokes = ids.Distinct().Where(id => Find(id) is InkElement).ToList();
        if (strokes.Count > 0)
            Execute(new EraseStrokesCommand(Page, strokes));
    });

    /// <summary>
    /// Called by the browser once a lasso line is closed. Selects every stroke with more than half of its points inside and every other element whose
    /// centre lies inside.
    /// </summary>
    /// <param name="polygon">The lasso line in page coordinates.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnLassoAsync(PointDto[] polygon) => InvokeAsync(async () =>
    {
        if (!Editable || polygon == null)
            return;
        var points = polygon.Where(p => p != null && double.IsFinite(p.X) && double.IsFinite(p.Y)).Select(p => (p.X, p.Y)).ToList();
        await SelectAsync(Page.Elements.Where(e => InkGeometry.InsideLasso(Measured(e), points)).Select(e => e.Id).ToList());
    });

    private NoteElement Measured(NoteElement element)
    {
        if (element is not (TextElement or TableElement) || element.Height > 0)
            return element;
        var copy = element.Clone();
        copy.Height = HeightOf(element);
        return copy;
    }
}

/// <summary>A point of a stroke reported by the browser in page coordinates (CSS pixels at 100 % zoom).</summary>
public sealed class InkPointDto
{
    /// <summary>The horizontal position.</summary>
    public double X { get; set; }

    /// <summary>The vertical position.</summary>
    public double Y { get; set; }

    /// <summary>The pen pressure from 0 to 1; 0.5 for a mouse.</summary>
    public double Pressure { get; set; }
}
