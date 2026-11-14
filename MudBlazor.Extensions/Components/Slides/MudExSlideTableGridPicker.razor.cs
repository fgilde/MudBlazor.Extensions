using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>A grid of up to 10 columns by 8 rows to pick the size of a new table, as in Insert → Table of PowerPoint.</summary>
public partial class MudExSlideTableGridPicker
{
    private const int MaxRows = 8;
    private const int MaxColumns = 10;

    private (int Rows, int Columns)? _hover;

    /// <summary>Raised with the number of rows and columns the user picked.</summary>
    [Parameter] public EventCallback<(int Rows, int Columns)> Chosen { get; set; }

    private string CellClass((int Rows, int Columns) size) =>
        _hover is { } hover && size.Rows <= hover.Rows && size.Columns <= hover.Columns
            ? "mud-ex-slide-table-picker-cell mud-ex-slide-table-picker-on"
            : "mud-ex-slide-table-picker-cell";
}
