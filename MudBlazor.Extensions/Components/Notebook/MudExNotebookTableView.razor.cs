using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>Shows a notebook table with fixed column widths; its height follows the cell text.</summary>
public partial class MudExNotebookTableView
{
    /// <summary>The table to show.</summary>
    [Parameter] public TableElement Table { get; set; }
}
