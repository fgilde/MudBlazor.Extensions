using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private static readonly (string Id, string Text, string Icon, ShapeAlignment Alignment)[] AlignItems =
    [
        ("pp-align-shapes-left", "Align Left", Icons.Material.Filled.AlignHorizontalLeft, ShapeAlignment.Left),
        ("pp-align-shapes-center", "Align Center", Icons.Material.Filled.AlignHorizontalCenter, ShapeAlignment.Center),
        ("pp-align-shapes-right", "Align Right", Icons.Material.Filled.AlignHorizontalRight, ShapeAlignment.Right),
        ("pp-align-shapes-top", "Align Top", Icons.Material.Filled.AlignVerticalTop, ShapeAlignment.Top),
        ("pp-align-shapes-middle", "Align Middle", Icons.Material.Filled.AlignVerticalCenter, ShapeAlignment.Middle),
        ("pp-align-shapes-bottom", "Align Bottom", Icons.Material.Filled.AlignVerticalBottom, ShapeAlignment.Bottom)
    ];

    private EventCallback<KeyboardEventArgs>? _arrangeKeyDown;
    private bool _alignToSlide;
    private MudMenu _shapeMenu;

    private EventCallback<KeyboardEventArgs> CanvasKeyDown =>
        _arrangeKeyDown ??= new EventCallback<KeyboardEventArgs>(WithoutRender.Instance, (Func<KeyboardEventArgs, Task>)OnCanvasKeyDownAsync);

    private IReadOnlyList<SlideShape> SelectedShapeList => _current == null
        ? []
        : SelectedShapes.Select(id => SlideShape.Flatten(_current.Shapes).FirstOrDefault(s => s.Id == id)).Where(s => s != null).ToList();

    private bool CanGroup => HasShapeSelection && SelectedShapeList is { Count: > 1 } shapes && shapes.Select(s => s.Parent).Distinct().Count() == 1
                             && !SlideShape.Flatten(shapes).Any(s => s.Placeholder != null || s is TableShape);

    private bool CanUngroup => HasShapeSelection && SelectedShapeList.Any(s => s is GroupShape);

    private bool CanDistribute => HasShapeSelection && SelectedShapes.Count >= (_alignToSlide ? 2 : 3);

    private bool CanRotate => HasShapeSelection && !SelectedShapeList.Any(s => s is TableShape);

    private async Task OnCanvasKeyDownAsync(KeyboardEventArgs e)
    {
        if ((e.CtrlKey || e.MetaKey) && !e.AltKey && e.Key is "g" or "G")
        {
            await (e.ShiftKey ? UngroupAsync() : GroupAsync());
            StateHasChanged();
            return;
        }

        await OnShortcutKeyDownAsync(e, false);
    }

    private async Task GroupAsync()
    {
        if (!CanGroup)
            return;
        var command = new GroupShapesCommand(_current, SelectedShapes.ToList());
        await RunAsync(() => _presentation.Execute(command));
        if (command.Group != null)
            await _canvas.SelectAsync([command.Group.Id]);
    }

    private async Task UngroupAsync()
    {
        if (!CanUngroup)
            return;
        var shapes = SelectedShapeList;
        var ids = shapes.SelectMany(s => s is GroupShape group ? group.Children.Select(c => c.Id) : [s.Id]).ToList();
        await RunAsync(() => _presentation.Execute(new UngroupShapesCommand(_current, shapes.OfType<GroupShape>().Select(g => g.Id).ToList())));
        await _canvas.SelectAsync(ids);
    }

    private Task AlignAsync(ShapeAlignment alignment) => RunAsync(() =>
    {
        if (HasShapeSelection)
            _presentation.Execute(new AlignShapesCommand(_current, SelectedShapes.ToList(), alignment, _alignToSlide));
    });

    private Task DistributeAsync(bool horizontal) => RunAsync(() =>
    {
        if (CanDistribute)
            _presentation.Execute(new DistributeShapesCommand(_current, SelectedShapes.ToList(), horizontal, _alignToSlide));
    });

    private Task RotateAsync(double degrees) => RunAsync(() =>
    {
        if (CanRotate)
            _presentation.Execute(new RotateShapesCommand(_current, SelectedShapes.ToList(), degrees));
    });

    private Task FlipAsync(bool horizontal) => RunAsync(() =>
    {
        if (CanRotate)
            _presentation.Execute(new FlipShapesCommand(_current, SelectedShapes.ToList(), horizontal));
    });

    private Task OpenShapeMenuAsync(MouseEventArgs e) => IsReadOnly || _shapeMenu == null || _current == null ? Task.CompletedTask : _shapeMenu.OpenMenuAsync(e);
}
