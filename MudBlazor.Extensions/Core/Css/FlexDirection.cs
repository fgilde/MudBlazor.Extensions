using System.ComponentModel;

namespace MudBlazor.Extensions.Core.Css;

/// <summary>
/// Values for the CSS <c>flex-direction</c> property.
/// </summary>
public enum FlexDirection
{
    /// <summary>Items are placed in a row from start to end (text direction).</summary>
    [Description("row")] Row,
    /// <summary>Items are placed in a row from end to start.</summary>
    [Description("row-reverse")] RowReverse,
    /// <summary>Items are placed in a column from top to bottom.</summary>
    [Description("column")] Column,
    /// <summary>Items are placed in a column from bottom to top.</summary>
    [Description("column-reverse")] ColumnReverse
}
