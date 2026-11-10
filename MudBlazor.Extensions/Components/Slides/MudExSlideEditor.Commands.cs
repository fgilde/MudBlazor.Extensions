using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private async Task RunAsync(Action action)
    {
        if (IsReadOnly)
            return;
        try
        {
            action();
        }
        catch (Exception e)
        {
            await ShowErrorAsync(e.Message);
        }
    }

    private Task AddSlideAsync(SlideLayout layout) => RunAsync(() =>
    {
        layout ??= DefaultLayout;
        if (layout == null)
            return;
        var command = new AddSlideCommand(_current == null ? 0 : _currentIndex + 1, layout);
        _presentation.Execute(command);
        Show(command.Slide);
    });

    private Task DuplicateSlidesAsync() => RunAsync(() =>
    {
        if (_selection.Count == 0)
            return;
        var command = new DuplicateSlidesCommand(_selection);
        _presentation.Execute(command);
        Show(command.Copies[0], command.Copies);
    });

    private Task DeleteSlidesAsync() => RunAsync(() =>
    {
        if (_selection.Count == 0)
            return;
        _currentIndex = _selection.Min(IndexOf);
        _presentation.Execute(new DeleteSlidesCommand(_selection));
    });

    private Task SetHiddenAsync(bool hidden) => RunAsync(() =>
    {
        if (_selection.Count > 0)
            _presentation.Execute(new SetSlideHiddenCommand(_selection, hidden));
    });

    private Task MoveSlidesAsync(int from, int target) => RunAsync(() =>
    {
        var slides = _presentation.Slides;
        if (from < 0 || from >= slides.Count || target < 0 || target > slides.Count)
            return;
        var dragged = slides[from];
        var moved = (_selection.Contains(dragged) ? _selection : [dragged]).OrderBy(IndexOf).ToList();
        var order = slides.Except(moved).ToList();
        order.InsertRange(target - moved.Count(s => IndexOf(s) < target), moved);
        if (order.SequenceEqual(slides))
            return;
        _presentation.Execute(new MoveSlidesCommand(moved, target));
        Show(dragged, moved);
    });

    private Task SetNotesAsync(string text) => RunAsync(() =>
    {
        if (_current != null && _presentation.CanHaveNotes(_current) && (text ?? "").ReplaceLineEndings("\n") != _current.Notes)
            _presentation.Execute(new SetNotesCommand(_current, text));
    });

    private Task UndoAsync(bool redo) => RunAsync(() =>
    {
        if (redo && _presentation.Undo.CanRedo)
            _presentation.Undo.Redo();
        else if (!redo && _presentation.Undo.CanUndo)
            _presentation.Undo.Undo();
    });

    private Task ArrangeAsync(ZOrderChange change) => RunAsync(() =>
    {
        if (_current != null && _shapeSelection.Count > 0)
            _presentation.Execute(new ReorderShapesCommand(_current, _shapeSelection, change));
    });

    private void OnSelectionChanged(SlideSelection selection) => Show(selection.Current, selection.Slides);

    private void SetZoom(double? zoom) => _zoom = zoom is { } z ? Math.Clamp(Math.Round(z, 2), 0.1, 4) : null;
}
