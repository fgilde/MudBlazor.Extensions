using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;

namespace MainSample.WebAssembly.Types;

public static class SampleDialogOptions
{
    public static DialogOptionsEx FullSlideFromRight => new()
    {
        MaximizeButton = true,
        CloseButton = true,
        FullHeight = true,
        CloseOnEscapeKey = true,
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        DragMode = MudDialogDragMode.Simple,
        Animations = new[] { AnimationType.SlideIn },
        Position = DialogPosition.CenterRight,
        DisableSizeMarginY = true,
        DisablePositionMargin = true
    };
}