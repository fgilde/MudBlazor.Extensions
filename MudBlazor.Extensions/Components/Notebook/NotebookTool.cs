namespace MudBlazor.Extensions.Components;

/// <summary>What a pointer gesture on the notebook page does.</summary>
public enum NotebookTool
{
    /// <summary>A click on the page writes text there; elements are selected and moved.</summary>
    Text,

    /// <summary>Elements are selected, moved and resized; dragging on the free page selects everything inside the frame.</summary>
    Select,

    /// <summary>Draws with a pen.</summary>
    Pen,

    /// <summary>Draws with a translucent highlighter.</summary>
    Highlighter,

    /// <summary>Erases whole strokes.</summary>
    Eraser,

    /// <summary>Selects strokes and elements with a freehand line.</summary>
    Lasso,

    /// <summary>Draws a shape.</summary>
    Shape
}
