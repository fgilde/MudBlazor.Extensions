namespace MudBlazor.Extensions.Office;

/// <summary>The chart types the editor can show and create.</summary>
public enum ChartKind
{
    /// <summary>Vertical bars.</summary>
    Column,

    /// <summary>Horizontal bars.</summary>
    Bar,

    /// <summary>Lines.</summary>
    Line,

    /// <summary>A pie.</summary>
    Pie,

    /// <summary>Filled areas.</summary>
    Area,

    /// <summary>X/Y points.</summary>
    Scatter,

    /// <summary>Any other chart read from a file. It is shown as a placeholder and saved unchanged, apart from its cell references.</summary>
    Other
}
