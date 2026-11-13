using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper.Internal;
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
    /// point is a dot. More than 100,000 values are ignored.
    /// </summary>
    /// <param name="points">The points in page coordinates as <c>x, y, pressure</c> triples, or null if they come as <paramref name="stream"/>.</param>
    /// <param name="stream">The points as a JSON array when they are too many for one message.</param>
    /// <returns>A task that completes when the stroke is added.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnStrokeAsync(double[] points, IJSStreamReference stream) => Guard(async () =>
    {
        var values = await JsPayload.ReadListAsync(points, stream);
        if (!Editable || !IsDrawing || values == null)
            return;
        var list = values.Chunk(3).Where(p => p.Length == 3 && double.IsFinite(p[0]) && double.IsFinite(p[1]))
            .Select(p => new InkPoint(Math.Max(0, p[0]), Math.Max(0, p[1]), double.IsFinite(p[2]) ? Math.Clamp(p[2], 0, 1) : 0.5))
            .ToList();
        if (list.Count > 0)
            Execute(new AddStrokeCommand(Page, InkGeometry.Simplify(list), EffectiveInkColor, EffectiveInkWidth,
                Tool == NotebookTool.Highlighter ? InkTool.Highlighter : InkTool.Pen));
    });

    /// <summary>Called by the browser once an eraser gesture ends, with every stroke it touched. They are deleted in one undo step.</summary>
    /// <param name="ids">The ids of the touched strokes, or null if they come as <paramref name="stream"/>; other ids are ignored.</param>
    /// <param name="stream">The ids as a JSON array when they are too many for one message.</param>
    /// <returns>A task that completes when the strokes are deleted.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnEraseAsync(string[] ids, IJSStreamReference stream) => Guard(async () =>
    {
        var list = await JsPayload.ReadListAsync(ids, stream);
        if (!Editable || list == null)
            return;
        var strokes = list.Distinct().Where(id => Find(id) is InkElement).ToList();
        if (strokes.Count > 0)
            Execute(new EraseStrokesCommand(Page, strokes));
    });

    /// <summary>
    /// Called by the browser once a lasso line is closed. Selects every stroke with more than half of its points inside and every other element whose
    /// centre lies inside.
    /// </summary>
    /// <param name="polygon">The lasso line in page coordinates as <c>x, y</c> pairs, or null if it comes as <paramref name="stream"/>.</param>
    /// <param name="stream">The line as a JSON array when it is too long for one message.</param>
    /// <returns>A task that completes when the selection is shown.</returns>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public Task OnLassoAsync(double[] polygon, IJSStreamReference stream) => Guard(async () =>
    {
        var values = await JsPayload.ReadListAsync(polygon, stream);
        if (!Editable || values == null)
            return;
        var points = values.Chunk(2).Where(p => p.Length == 2 && double.IsFinite(p[0]) && double.IsFinite(p[1])).Select(p => (p[0], p[1])).ToList();
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
